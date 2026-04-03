import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { AvatarModel, AvatarService } from '../modules/avatar/services/avatar.service';
import { LookCollectionModel, LooksService } from '../modules/looks/services/looks.service';
import { GamificationSummary, UserProfile, UserService } from '../modules/user/services/user.service';

interface DashboardLook {
  id: string;
  name: string;
  occasion: string;
  style: string;
  totalPrice: number;
  heroImageUrl?: string | null;
  collections?: string[];
}

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class HomePage {
  user: UserProfile | null = null;
  primaryAvatar: AvatarModel | null = null;
  recentLooks: DashboardLook[] = [];
  lookCollections: LookCollectionModel[] = [];
  gamification: GamificationSummary | null = null;
  loading = true;
  error = '';

  readonly quickActions = [
    { label: 'Criar avatar', icon: 'person-circle-outline', route: '/avatar/create' },
    { label: 'Meus avatares', icon: 'body-outline', route: '/avatar/list' },
    { label: 'Ver looks', icon: 'sparkles-outline', route: '/looks/list' },
    { label: 'Explorar loja', icon: 'bag-handle-outline', route: '/shop/list' }
  ];

  readonly focusCards = [
    {
      eyebrow: 'Seu proximo passo',
      title: 'Complete a base do seu estilo',
      text: 'Um avatar bem ajustado melhora o caminho para looks e recomendacoes futuras.',
      icon: 'body-outline',
      route: '/avatar/list'
    },
    {
      eyebrow: 'Descoberta',
      title: 'Navegue pelo catalogo do MVP',
      text: 'Use a loja para validar preferencia visual, faixa de preco e intencao de compra.',
      icon: 'storefront-outline',
      route: '/shop/list'
    }
  ];

  constructor(
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private avatarService: AvatarService,
    private looksService: LooksService
  ) {}

  async ionViewWillEnter(): Promise<void> {
    await this.loadDashboard();
  }

  async ngOnInit(): Promise<void> {
    await this.loadDashboard();
  }

  async loadDashboard(): Promise<void> {
    this.loading = true;
    this.error = '';

    try {
      const [user, avatarResponse, looksResponse, collectionsResponse, gamification] = await Promise.all([
        firstValueFrom(this.userService.getProfile()),
        firstValueFrom(this.avatarService.getUserAvatars()),
        firstValueFrom(this.looksService.getUserLooks()),
        firstValueFrom(this.looksService.getLookCollections()),
        firstValueFrom(this.userService.getGamificationSummary())
      ]);

      this.user = user;
      this.primaryAvatar = avatarResponse.data.length > 0 ? avatarResponse.data[0] : null;
      this.lookCollections = collectionsResponse.data;
      this.gamification = gamification;
      this.recentLooks = (looksResponse?.data ?? []).slice(0, 2).map(look => ({
        ...look,
        collections: this.getCollectionLabelsForLook(look.id)
      }));
    } catch (error) {
      this.error = 'Nao foi possivel carregar sua dashboard agora.';
      console.error(error);
    } finally {
      this.loading = false;
    }
  }

  navigate(route: string): void {
    this.router.navigateByUrl(route);
  }

  async logout(): Promise<void> {
    await firstValueFrom(this.authService.logout());
    await this.router.navigateByUrl('/auth/login', { replaceUrl: true });
  }

  get firstName(): string {
    return this.user?.firstName || 'Style lover';
  }

  get avatarSummary(): string {
    if (!this.primaryAvatar) {
      return 'Crie seu primeiro avatar para personalizar a experiencia.';
    }

    return `${this.primaryAvatar.bodyType} • ${this.primaryAvatar.skinTone} • ${this.primaryAvatar.height} cm`;
  }

  get primaryAvatarImageUrl(): string {
    return this.avatarService.resolveAvatarImageUrl(this.primaryAvatar);
  }

  get primaryAvatarVisualLabel(): string {
    return this.avatarService.resolveAvatarVisualLabel(this.primaryAvatar);
  }

  get lookCountLabel(): string {
    if (this.recentLooks.length === 0) {
      return 'Nenhum look salvo ainda';
    }

    if (this.recentLooks.length === 1) {
      return '1 look recente';
    }

    return `${this.recentLooks.length} looks recentes`;
  }

  get missionPreview() {
    return this.gamification?.missions.slice(0, 2) ?? [];
  }

  private getCollectionLabelsForLook(lookId: string): string[] {
    return this.lookCollections
      .filter(collection => collection.looks.some(look => look.id === lookId))
      .map(collection => collection.label)
      .slice(0, 3);
  }
}
