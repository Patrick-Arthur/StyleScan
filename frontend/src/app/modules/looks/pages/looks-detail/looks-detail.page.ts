import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AlertController, IonicModule, ToastController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ShareService } from 'src/app/core/services/share.service';
import { environment } from 'src/environments/environment';
import { UserService } from 'src/app/modules/user/services/user.service';
import { LookCollectionModel, LookItem, LookModel, LooksService } from '../../services/looks.service';

interface StorePurchaseGroup {
  name: string;
  url: string;
  subtotal: number;
  items: LookItem[];
}

@Component({
  selector: 'app-looks-detail',
  templateUrl: './looks-detail.page.html',
  styleUrls: ['./looks-detail.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule]
})
export class LooksDetailPage implements OnInit {
  look: LookModel | null = null;
  loading = true;
  error = '';
  savingDetails = false;
  publishing = false;
  editableName = '';
  editableNote = '';
  editableTags = '';
  collections: LookCollectionModel[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private looksService: LooksService,
    private userService: UserService,
    private shareService: ShareService,
    private toastController: ToastController,
    private alertController: AlertController
  ) {}

  ngOnInit(): void {
    void this.loadLook();
  }

  async loadLook(): Promise<void> {
    const lookId = this.route.snapshot.paramMap.get('id');
    if (!lookId) {
      this.error = 'Look invalido.';
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      const [look, collectionsResponse] = await Promise.all([
        firstValueFrom(this.looksService.getLookById(lookId)),
        firstValueFrom(this.looksService.getLookCollections())
      ]);
      this.look = look;
      this.collections = collectionsResponse.data;
      this.syncEditableFields();
    } catch (error) {
      console.error(error);
      this.error = 'Nao foi possivel carregar esse look.';
    } finally {
      this.loading = false;
    }
  }

  get heroImages(): string[] {
    if (this.look?.heroImageUrl) {
      return [this.look.heroImageUrl, ...this.look.items.slice(0, 2).map(item => item.imageUrl)];
    }

    return this.look?.items.slice(0, 3).map(item => item.imageUrl) ?? [];
  }

  get purchaseGroups(): StorePurchaseGroup[] {
    if (!this.look) {
      return [];
    }

    const groups = new Map<string, StorePurchaseGroup>();

    for (const item of this.look.items) {
      const key = `${item.storeName}|${item.productUrl}`;
      const existing = groups.get(key);
      if (existing) {
        existing.items.push(item);
        existing.subtotal += item.price;
        continue;
      }

      groups.set(key, {
        name: item.storeName,
        url: item.productUrl,
        subtotal: item.price,
        items: [item]
      });
    }

    return Array.from(groups.values());
  }

  get parsedTags(): string[] {
    return this.editableTags
      .split(',')
      .map(tag => tag.trim())
      .filter((tag, index, list) => tag.length > 0 && list.indexOf(tag) === index)
      .slice(0, 6);
  }

  get hasDetailChanges(): boolean {
    if (!this.look) {
      return false;
    }

    return this.editableName.trim() !== this.look.name
      || (this.editableNote.trim() || '') !== (this.look.note ?? '')
      || this.parsedTags.join('|') !== (this.look.occasionTags ?? []).join('|');
  }

  get lookCollectionLabels(): string[] {
    if (!this.look) {
      return [];
    }

    return this.collections
      .filter(collection => collection.looks.some(look => look.id === this.look?.id))
      .map(collection => collection.label);
  }

  get publicationStatusLabel(): string {
    return this.look?.isPublished ? 'Publicado no perfil' : 'Privado';
  }

  get publicLookUrl(): string {
    if (!this.look?.isPublished || !this.look.shareSlug) {
      return '';
    }

    return `${environment.publicSiteUrl}/look/${this.look.shareSlug}`;
  }

  get lookManualCollections(): LookCollectionModel[] {
    if (!this.look) {
      return [];
    }

    return this.collections.filter(collection => collection.looks.some(look => look.id === this.look?.id));
  }

  reopenInStudio(): void {
    if (!this.look) {
      return;
    }

    this.router.navigate(['/looks/list'], {
      queryParams: { lookId: this.look.id }
    });
  }

  openStore(url: string): void {
    if (!url) {
      return;
    }

    window.open(url, '_blank', 'noopener');
  }

  buyFullLook(): void {
    this.purchaseGroups.forEach(group => this.openStore(group.url));
    void this.registerPurchaseIntent();
  }

  async saveDetails(): Promise<void> {
    if (!this.look || !this.editableName.trim() || !this.hasDetailChanges || this.savingDetails) {
      return;
    }

    this.savingDetails = true;
    try {
      this.look = await firstValueFrom(this.looksService.updateLookDetails(this.look.id, {
        name: this.editableName.trim(),
        note: this.editableNote.trim() || undefined,
        occasionTags: this.parsedTags
      }));
      this.syncEditableFields();
      await this.presentToast('Look atualizado com sucesso.');
    } catch (error) {
      console.error(error);
      await this.presentToast('Nao foi possivel atualizar esse look.');
    } finally {
      this.savingDetails = false;
    }
  }

  async addToCollection(): Promise<void> {
    if (!this.look) {
      return;
    }

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

            try {
              await firstValueFrom(this.looksService.addLookToCollection(this.look!.id, name));
              await this.reloadCollections();
              await this.presentToast('Look adicionado a colecao.');
              return true;
            } catch (error) {
              console.error(error);
              await this.presentToast('Nao foi possivel adicionar a colecao.');
              return false;
            }
          }
        }
      ]
    });

    await alert.present();
  }

  async removeFromCollection(collection: LookCollectionModel): Promise<void> {
    if (!this.look) {
      return;
    }

    try {
      await firstValueFrom(this.looksService.removeLookFromCollection(this.look.id, collection.id));
      await this.reloadCollections();
      await this.presentToast('Look removido da colecao.');
    } catch (error) {
      console.error(error);
      await this.presentToast('Nao foi possivel remover da colecao.');
    }
  }

  async shareLook(): Promise<void> {
    if (!this.look) {
      return;
    }

    const shared = await this.shareService.share({
      title: `StyleScan | ${this.look.name}`,
      text: `Olha esse look que montei no StyleScan: ${this.look.name} • ${this.look.style} • ${this.look.occasion}`,
      url: this.publicLookUrl || window.location.href,
      imageUrl: this.look.heroImageUrl || undefined
    });

    if (!shared) {
      await this.presentToast('Nao foi possivel compartilhar esse look agora.');
      return;
    }

    await this.registerSharedLook();
    await this.presentToast('Look pronto para compartilhar.');
  }

  async togglePublication(): Promise<void> {
    if (!this.look || this.publishing) {
      return;
    }

    this.publishing = true;
    try {
      this.look = await firstValueFrom(this.looksService.updateLookPublication(this.look.id, {
        isPublished: !this.look.isPublished
      }));
      await this.presentToast(this.look.isPublished
        ? 'Look publicado na sua vitrine do perfil.'
        : 'Look ocultado da sua vitrine publicada.');
    } catch (error) {
      console.error(error);
      await this.presentToast('Nao foi possivel atualizar a publicacao desse look.');
    } finally {
      this.publishing = false;
    }
  }

  trackByStore(_: number, group: StorePurchaseGroup): string {
    return `${group.name}-${group.url}`;
  }

  private syncEditableFields(): void {
    if (!this.look) {
      return;
    }

    this.editableName = this.look.name;
    this.editableNote = this.look.note ?? '';
    this.editableTags = (this.look.occasionTags ?? []).join(', ');
  }

  private async reloadCollections(): Promise<void> {
    const collectionsResponse = await firstValueFrom(this.looksService.getLookCollections());
    this.collections = collectionsResponse.data;
  }

  private async registerSharedLook(): Promise<void> {
    try {
      await firstValueFrom(this.userService.registerUsage('shared_look'));
    } catch (error) {
      console.error(error);
    }
  }

  private async registerPurchaseIntent(): Promise<void> {
    try {
      await firstValueFrom(this.userService.registerUsage('purchase_click'));
    } catch (error) {
      console.error(error);
    }
  }

  private async presentToast(message: string): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 2200,
      position: 'top'
    });

    await toast.present();
  }
}

