import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AlertController, IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { LookCollectionModel, LookModel, LooksService } from 'src/app/modules/looks/services/looks.service';
import { CurrencyPipe } from 'src/app/shared/pipes/currency.pipe';

interface FavoriteCollection {
  id: string;
  label: string;
  looks: LookModel[];
}

@Component({
  selector: 'app-favorites',
  templateUrl: './favorites.page.html',
  styleUrls: ['./favorites.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule, CurrencyPipe]
})
export class FavoritesPage {
  favoriteLooks: LookModel[] = [];
  manualCollections: LookCollectionModel[] = [];
  loading = false;
  searchTerm = '';
  selectedCollection = 'all';

  constructor(
    private router: Router,
    private looksService: LooksService,
    private alertController: AlertController
  ) {}

  async ionViewWillEnter(): Promise<void> {
    this.loading = true;
    try {
      await this.loadFavoritesData();
    } finally {
      this.loading = false;
    }
  }

  get selectedManualCollection(): LookCollectionModel | null {
    if (!this.selectedCollection.startsWith('manual:')) {
      return null;
    }

    const collectionId = this.selectedCollection.replace('manual:', '');
    return this.manualCollections.find(collection => collection.id === collectionId) ?? null;
  }

  get collections(): FavoriteCollection[] {
    const collectionMap = new Map<string, FavoriteCollection>();

    for (const look of this.favoriteLooks) {
      const tags = (look.occasionTags ?? []).slice(0, 2);
      const keys = tags.length > 0 ? tags : [look.occasion];

      for (const key of keys) {
        const normalizedKey = key.toLowerCase();
        const existing = collectionMap.get(normalizedKey);
        if (existing) {
          existing.looks.push(look);
          continue;
        }

        collectionMap.set(normalizedKey, {
          id: normalizedKey,
          label: key,
          looks: [look]
        });
      }
    }

    return Array.from(collectionMap.values()).sort((left, right) => right.looks.length - left.looks.length || left.label.localeCompare(right.label));
  }

  get filteredFavoriteLooks(): LookModel[] {
    const search = this.searchTerm.trim().toLowerCase();

    return this.favoriteLooks.filter(look => {
      const matchesManualCollection = this.selectedCollection.startsWith('manual:')
        ? this.manualCollections.some(collection =>
            collection.id === this.selectedCollection.replace('manual:', '')
            && collection.looks.some(item => item.id === look.id))
        : false;

      const matchesCollection = this.selectedCollection === 'all'
        || matchesManualCollection
        || look.occasion.toLowerCase() === this.selectedCollection
        || (look.occasionTags ?? []).some(tag => tag.toLowerCase() === this.selectedCollection);

      const matchesSearch = !search || [
        look.name,
        look.style,
        look.occasion,
        look.note ?? '',
        ...(look.occasionTags ?? [])
      ].some(value => value.toLowerCase().includes(search));

      return matchesCollection && matchesSearch;
    });
  }

  async removeFavorite(lookId: string): Promise<void> {
    await firstValueFrom(this.looksService.removeLookFromFavorites(lookId));
    this.favoriteLooks = this.favoriteLooks.filter(look => look.id !== lookId);
    this.manualCollections = this.manualCollections
      .map(collection => ({
        ...collection,
        looks: collection.looks.filter(look => look.id !== lookId)
      }))
      .filter(collection => collection.looks.length > 0);

    if (this.selectedManualCollection && !this.manualCollections.some(collection => collection.id === this.selectedManualCollection?.id)) {
      this.selectedCollection = 'all';
    }
  }

  viewLookDetail(lookId: string): void {
    this.router.navigate(['/looks', lookId]);
  }

  isLookInCollection(lookId: string, collectionId: string): boolean {
    return this.manualCollections.some(collection => collection.id === collectionId && collection.looks.some(look => look.id === lookId));
  }

  async addLookToCollection(look: LookModel): Promise<void> {
    const alert = await this.alertController.create({
      header: 'Adicionar a colecao',
      inputs: [
        {
          name: 'name',
          type: 'text',
          placeholder: 'Ex.: Trabalho, Noite, Viagem'
        }
      ],
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Salvar',
          handler: async value => {
            const name = (value?.name ?? '').trim();
            if (!name) {
              return false;
            }

            await firstValueFrom(this.looksService.addLookToCollection(look.id, name));
            await this.reloadManualCollections();
            return true;
          }
        }
      ]
    });

    await alert.present();
  }

  async removeLookFromCollection(lookId: string, collectionId: string): Promise<void> {
    await firstValueFrom(this.looksService.removeLookFromCollection(lookId, collectionId));
    await this.reloadManualCollections();
  }

  async renameSelectedCollection(): Promise<void> {
    const collection = this.selectedManualCollection;
    if (!collection) {
      return;
    }

    const alert = await this.alertController.create({
      header: 'Renomear colecao',
      inputs: [
        {
          name: 'name',
          type: 'text',
          value: collection.label,
          placeholder: 'Novo nome da colecao'
        }
      ],
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Salvar',
          handler: async value => {
            const name = (value?.name ?? '').trim();
            if (!name) {
              return false;
            }

            await firstValueFrom(this.looksService.renameLookCollection(collection.id, name));
            await this.reloadManualCollections();
            const refreshed = this.manualCollections.find(item => item.label.toLowerCase() === name.toLowerCase());
            this.selectedCollection = refreshed ? `manual:${refreshed.id}` : 'all';
            return true;
          }
        }
      ]
    });

    await alert.present();
  }

  async deleteSelectedCollection(): Promise<void> {
    const collection = this.selectedManualCollection;
    if (!collection) {
      return;
    }

    const alert = await this.alertController.create({
      header: 'Excluir colecao',
      message: `A colecao "${collection.label}" sera removida dos favoritos, mas os looks continuam salvos.`,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: async () => {
            await firstValueFrom(this.looksService.deleteLookCollection(collection.id));
            this.selectedCollection = 'all';
            await this.reloadManualCollections();
          }
        }
      ]
    });

    await alert.present();
  }

  private async loadFavoritesData(): Promise<void> {
    const [favoritesResponse, collectionsResponse] = await Promise.all([
      firstValueFrom(this.looksService.getFavoriteLooks()),
      firstValueFrom(this.looksService.getLookCollections())
    ]);
    this.favoriteLooks = favoritesResponse.data;
    this.manualCollections = collectionsResponse.data;
  }

  private async reloadManualCollections(): Promise<void> {
    const collectionsResponse = await firstValueFrom(this.looksService.getLookCollections());
    this.manualCollections = collectionsResponse.data;

    if (this.selectedCollection.startsWith('manual:')) {
      const collectionId = this.selectedCollection.replace('manual:', '');
      const stillExists = this.manualCollections.some(collection => collection.id === collectionId);
      if (!stillExists) {
        this.selectedCollection = 'all';
      }
    }
  }
}
