import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService } from 'src/app/core/services/account-plan.service';
import { AvatarModel, AvatarService } from '../../services/avatar.service';

@Component({
  selector: 'app-avatar-list',
  templateUrl: './avatar-list.page.html',
  styleUrls: ['./avatar-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class AvatarListPage implements OnInit {
  avatars: AvatarModel[] = [];
  loading = false;
  error = '';

  constructor(
    private router: Router,
    private avatarService: AvatarService,
    private accountPlanService: AccountPlanService
  ) {}

  ngOnInit(): void {
    this.loadAvatars();
  }

  async ionViewWillEnter(): Promise<void> {
    await this.loadAvatars();
  }

  async loadAvatars(): Promise<void> {
    this.loading = true;
    this.error = '';

    try {
      const [response] = await Promise.all([
        firstValueFrom(this.avatarService.getUserAvatars()),
        firstValueFrom(this.accountPlanService.loadRemoteState())
      ]);
      this.avatars = response.data;
    } catch (error) {
      this.error = 'Nao foi possivel carregar seus avatares.';
      console.error(error);
    } finally {
      this.loading = false;
    }
  }

  goToCreateAvatar(): void {
    const limitCheck = this.accountPlanService.canCreateAvatar(this.avatars.length);
    if (!limitCheck.allowed) {
      this.error = `${limitCheck.reason} ${this.accountPlanService.getUpgradePrompt('avatarSlots')}`;
      return;
    }

    this.router.navigateByUrl('/avatar/create');
  }

  goHome(): void {
    this.router.navigateByUrl('/home');
  }

  customizeAvatar(avatarId: string): void {
    this.router.navigateByUrl(`/avatar/customize/${avatarId}`);
  }

  getAvatarImageUrl(avatar: AvatarModel): string {
    return this.avatarService.resolveAvatarImageUrl(avatar);
  }

  getAvatarVisualLabel(avatar: AvatarModel): string {
    return this.avatarService.resolveAvatarVisualLabel(avatar);
  }

  goToUpgrade(): void {
    this.router.navigateByUrl('/user/upgrade');
  }

  get currentPlanName(): string {
    return this.accountPlanService.getCurrentPlan().name;
  }

  get avatarSlotLabel(): string {
    const limit = this.accountPlanService.getCurrentPlan().limits.avatars;
    return `${this.avatars.length}/${limit} avatares ativos`;
  }

  get canCreateAvatar(): boolean {
    return this.accountPlanService.canCreateAvatar(this.avatars.length).allowed;
  }

  get avatarUpgradeTitle(): string {
    const remaining = this.remainingAvatarSlots;
    if (remaining <= 0) {
      return 'Seu limite de avatares foi atingido';
    }

    if (remaining === 1) {
      return 'Voce esta usando o ultimo slot disponivel';
    }

    return 'Ainda cabe expandir sua biblioteca';
  }

  get avatarUpgradeCopy(): string {
    const plan = this.accountPlanService.getCurrentPlan();
    const remaining = this.remainingAvatarSlots;

    if (remaining <= 0) {
      return `${plan.name} chegou ao limite de avatares ativos. Um upgrade libera mais bases para testes, estilos e comparacoes.`;
    }

    if (remaining === 1) {
      return `Seu plano ${plan.name} esta perto do limite. Vale abrir os planos premium antes do proximo avatar.`;
    }

    return 'Planos premium liberam mais slots ativos e deixam sua biblioteca pronta para jornadas diferentes de estilo.';
  }

  get showAvatarUpgradeCard(): boolean {
    const limit = this.accountPlanService.getCurrentPlan().limits.avatars;
    return this.avatars.length >= Math.max(limit - 1, 0);
  }

  private get remainingAvatarSlots(): number {
    return Math.max(this.accountPlanService.getCurrentPlan().limits.avatars - this.avatars.length, 0);
  }
}
