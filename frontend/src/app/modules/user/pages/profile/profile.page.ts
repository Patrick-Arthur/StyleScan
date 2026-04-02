import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService } from 'src/app/core/services/account-plan.service';
import { ShareService } from 'src/app/core/services/share.service';
import { LookModel, LooksService } from 'src/app/modules/looks/services/looks.service';
import { environment } from 'src/environments/environment';
import { UserProfile, UserService } from '../../services/user.service';

interface UsageCard {
  label: string;
  detail: string;
  progress: number;
}

@Component({
  selector: 'app-profile',
  templateUrl: './profile.page.html',
  styleUrls: ['./profile.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule]
})
export class ProfilePage implements OnInit {
  user: UserProfile | null = null;
  looks: LookModel[] = [];
  loading = true;
  saving = false;
  feedback = '';
  error = '';
  activationHighlight = '';

  constructor(
    private router: Router,
    private userService: UserService,
    private accountPlanService: AccountPlanService,
    private looksService: LooksService,
    private shareService: ShareService
  ) {}

  ngOnInit(): void {
    const navigationState = window.history.state as { subscriptionActivated?: boolean };
    if (navigationState?.subscriptionActivated) {
      this.activationHighlight = 'Seu plano premium foi liberado e os novos limites ja estao ativos na conta.';
    }
    this.loadProfile();
  }

  async ionViewWillEnter(): Promise<void> {
    await this.loadProfile();
  }

  goHome(): void {
    this.router.navigateByUrl('/home');
  }

  goToUpgrade(): void {
    this.router.navigateByUrl('/user/upgrade');
  }

  openLook(lookId: string): void {
    this.router.navigate(['/looks', lookId]);
  }

  get publicProfileUrl(): string {
    if (!this.user?.publicProfileSlug) {
      return '';
    }

    return `${environment.publicSiteUrl}/p/${this.user.publicProfileSlug}`;
  }

  async sharePublicProfile(): Promise<void> {
    if (!this.user?.publicProfileSlug) {
      return;
    }

    const shared = await this.shareService.shareNative({
      title: `Vitrine StyleScan de ${this.displayName}`,
      text: 'Veja meus looks publicados no StyleScan.',
      url: this.publicProfileUrl
    });

    this.feedback = shared
      ? 'Vitrine publica preparada para compartilhar.'
      : 'Nao foi possivel compartilhar sua vitrine agora.';
  }

  async copyPublicProfileLink(): Promise<void> {
    if (!this.user?.publicProfileSlug) {
      return;
    }

    try {
      await navigator.clipboard.writeText(this.publicProfileUrl);
      this.feedback = 'Link da vitrine publica copiado.';
    } catch {
      this.error = 'Nao foi possivel copiar o link da vitrine.';
    }
  }

  async loadProfile(): Promise<void> {
    this.loading = true;
    this.error = '';
    this.feedback = '';

    try {
      const [profile, looksResponse] = await Promise.all([
        firstValueFrom(this.userService.getProfile()),
        firstValueFrom(this.looksService.getUserLooks())
      ]);
      this.user = profile;
      this.looks = looksResponse.data;
      this.accountPlanService.hydrateFromProfile(this.user);
      if ((this.user.subscription?.lastPaymentStatus || this.user.subscription?.status || '').toLowerCase() === 'approved') {
        this.activationHighlight = 'Pagamento confirmado. Seu plano premium ja pode ser usado nas telas do app.';
      }
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel carregar o perfil agora.';
    } finally {
      this.loading = false;
    }
  }

  async saveProfile(): Promise<void> {
    if (!this.user || this.saving) {
      return;
    }

    this.saving = true;
    this.error = '';
    this.feedback = '';

    try {
      this.user = await firstValueFrom(this.userService.updateProfile({
        firstName: this.user.firstName,
        lastName: this.user.lastName,
        dateOfBirth: this.user.dateOfBirth,
        gender: this.user.gender || null
      }));
      this.accountPlanService.hydrateFromProfile(this.user);
      this.feedback = 'Perfil atualizado com sucesso.';
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel salvar o perfil agora.';
    } finally {
      this.saving = false;
    }
  }

  get displayName(): string {
    if (!this.user) {
      return 'Seu perfil';
    }

    return `${this.user.firstName} ${this.user.lastName}`.trim();
  }

  get currentPlanName(): string {
    return this.accountPlanService.getCurrentPlan().name;
  }

  get currentPlanSummary(): string {
    return this.accountPlanService.getCurrentPlan().summary;
  }

  get isPremiumPlan(): boolean {
    return this.accountPlanService.getCurrentPlan().id !== 'free';
  }

  get unlockedHighlights(): string[] {
    if (!this.user) {
      return [];
    }

    const limits = this.user.limits;
    return [
      `${limits.avatars} avatar${limits.avatars > 1 ? 'es' : ''} ativo${limits.avatars > 1 ? 's' : ''}`,
      `${limits.avatarTryOnsPerWeek} provas no avatar por semana`,
      `${limits.realisticRendersPerMonth} fotos realistas por mes`,
      limits.savedLooks >= 9999 ? 'Looks salvos ilimitados' : `${limits.savedLooks} looks salvos`
    ];
  }

  get subscriptionStatusLabel(): string {
    const status = this.user?.subscription?.status?.trim().toLowerCase();

    switch (status) {
      case 'approved':
      case 'active':
        return 'Pagamento confirmado';
      case 'pending':
        return 'Aguardando confirmacao';
      case 'authorized':
        return 'Pagamento autorizado';
      case 'rejected':
        return 'Pagamento recusado';
      case 'cancelled':
      case 'cancelled_by_user':
        return 'Pagamento cancelado';
      default:
        return 'Perfil autenticado';
    }
  }

  get publishedLooks(): LookModel[] {
    return this.looks
      .filter(look => look.isPublished)
      .sort((left, right) => {
        const rightTime = new Date(right.publishedAt || right.createdAt).getTime();
        const leftTime = new Date(left.publishedAt || left.createdAt).getTime();
        return rightTime - leftTime;
      });
  }

  formatDateTime(value?: string | null): string {
    if (!value) {
      return 'Ainda nao registrado';
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return value;
    }

    return parsed.toLocaleString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  get usageCards(): UsageCard[] {
    if (!this.user) {
      return [];
    }

    const usageMap = new Map(this.user.usage.map(item => [item.metricType, item.used]));
    const limits = this.user.limits;

    return [
      this.buildUsageCard('Avatares ativos', usageMap.get('avatar_slot') ?? 0, limits.avatars, 'em uso'),
      this.buildUsageCard('Provas no avatar', usageMap.get('avatar_try_on') ?? 0, limits.avatarTryOnsPerWeek, 'na semana'),
      this.buildUsageCard('Fotos realistas', usageMap.get('realistic_render') ?? 0, limits.realisticRendersPerMonth, 'no mes'),
      this.buildUsageCard('Looks salvos', usageMap.get('saved_look') ?? 0, limits.savedLooks, 'no acervo')
    ];
  }

  async refreshSubscriptionStatus(): Promise<void> {
    if (!this.user) {
      return;
    }

    this.error = '';
    this.feedback = '';

    try {
      const subscription = await firstValueFrom(this.userService.getSubscriptionStatus());
      this.user = {
        ...this.user,
        subscription
      };
      this.feedback = 'Status da assinatura atualizado.';
      if ((subscription.lastPaymentStatus || subscription.status || '').toLowerCase() === 'approved') {
        this.activationHighlight = 'Pagamento confirmado. Seu plano premium ja pode ser usado nas telas do app.';
      }
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel atualizar o status da assinatura.';
    }
  }

  private buildUsageCard(label: string, used: number, limit: number, suffix: string): UsageCard {
    const cappedLimit = limit >= 9999 ? Math.max(used, 1) : Math.max(limit, 1);
    return {
      label,
      detail: limit >= 9999 ? `${used} ${suffix} / ilimitado` : `${used}/${limit} ${suffix}`,
      progress: Math.min((used / cappedLimit) * 100, 100)
    };
  }
}
