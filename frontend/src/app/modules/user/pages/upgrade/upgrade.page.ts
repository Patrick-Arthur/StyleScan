import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { IonicModule } from '@ionic/angular';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AccountPlanDefinition, AccountPlanId, AccountPlanService } from 'src/app/core/services/account-plan.service';
import { SubscriptionCheckoutSession, UserService } from '../../services/user.service';

interface UpgradePlanLimit {
  label: string;
  value: string;
}

interface UpgradePlan {
  id: AccountPlanId;
  name: string;
  price: string;
  cadence: string;
  summary: string;
  ribbon?: string;
  featured?: boolean;
  accent: 'soft' | 'aqua' | 'midnight';
  limits: UpgradePlanLimit[];
  perks: string[];
}

interface UpgradeUsageRow {
  label: string;
  detail: string;
  progress: number;
}

@Component({
  selector: 'app-upgrade',
  templateUrl: './upgrade.page.html',
  styleUrls: ['./upgrade.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class UpgradePage {
  switchingPlanId: AccountPlanId | null = null;
  error = '';
  success = '';

  readonly plans: UpgradePlan[] = [
    {
      id: 'plus',
      name: 'Style Plus',
      price: 'R$ 1,00',
      cadence: '/mes',
      summary: 'Mais liberdade para testar estilos e montar uma rotina de looks no app.',
      accent: 'soft',
      limits: [
        { label: 'Avatares ativos', value: '3' },
        { label: 'Provas no avatar', value: '24 / semana' },
        { label: 'Fotos realistas', value: '12 / mes' },
        { label: 'Looks salvos', value: '30' }
      ],
      perks: [
        'Historico ampliado de looks',
        'Acesso a novas vitrines de estilo primeiro',
        'Mais tentativas para testar combinacoes'
      ]
    },
    {
      id: 'pro',
      name: 'Style Pro',
      price: 'R$ 39,90',
      cadence: '/mes',
      summary: 'Plano pensado para quem vai usar o provador com frequencia e quer mais profundidade.',
      ribbon: 'Mais popular',
      featured: true,
      accent: 'aqua',
      limits: [
        { label: 'Avatares ativos', value: '8' },
        { label: 'Provas no avatar', value: '60 / semana' },
        { label: 'Fotos realistas', value: '30 / mes' },
        { label: 'Looks salvos', value: 'Ilimitados' }
      ],
      perks: [
        'Fila prioritaria para geracoes premium',
        'Colecoes sazonais desbloqueadas',
        'Comparativo de looks mais completo'
      ]
    },
    {
      id: 'atelier',
      name: 'Style Atelier',
      price: 'R$ 79,90',
      cadence: '/mes',
      summary: 'Camada premium para usuarios intensivos e para uma experiencia mais exclusiva.',
      accent: 'midnight',
      limits: [
        { label: 'Avatares ativos', value: '15' },
        { label: 'Provas no avatar', value: '140 / semana' },
        { label: 'Fotos realistas', value: '80 / mes' },
        { label: 'Looks salvos', value: 'Ilimitados' }
      ],
      perks: [
        'Acesso antecipado a recursos de IA',
        'Studio com boards premium liberados',
        'Prioridade maxima em geracoes e novidades'
      ]
    }
  ];

  constructor(
    private router: Router,
    private accountPlanService: AccountPlanService,
    private userService: UserService
  ) {}

  get currentPlan(): AccountPlanDefinition {
    return this.accountPlanService.getCurrentPlan();
  }

  get currentPlanLimits(): string[] {
    const { limits } = this.currentPlan;
    return [
      `${limits.avatars} avatar${limits.avatars > 1 ? 'es' : ''} ativo${limits.avatars > 1 ? 's' : ''}`,
      `${limits.avatarTryOnsPerWeek} provas no avatar por semana`,
      `${limits.realisticRendersPerMonth} fotos realistas por mes`,
      limits.savedLooks >= 9999 ? 'Looks salvos ilimitados' : `${limits.savedLooks} looks salvos`
    ];
  }

  get usageRows(): UpgradeUsageRow[] {
    const usage = this.accountPlanService.getUsage();
    const { limits } = this.currentPlan;

    return [
      this.buildUsageRow('Avatares ativos', usage.avatarSlots.used, limits.avatars, 'ativos'),
      this.buildUsageRow('Provas no avatar', usage.avatarTryOns.used, limits.avatarTryOnsPerWeek, 'na semana'),
      this.buildUsageRow('Fotos realistas', usage.realisticRenders.used, limits.realisticRendersPerMonth, 'no mes'),
      this.buildUsageRow('Looks salvos', usage.savedLooks.used, limits.savedLooks, 'no acervo')
    ];
  }

  goBack(): void {
    this.router.navigateByUrl('/user/profile');
  }

  isCurrentPlan(planId: AccountPlanId): boolean {
    return this.currentPlan.id === planId;
  }

  async choosePlan(planId: AccountPlanId): Promise<void> {
    if (this.switchingPlanId) {
      return;
    }

    this.switchingPlanId = planId;
    this.error = '';
    this.success = '';

    try {
      const checkout: SubscriptionCheckoutSession = await firstValueFrom(this.userService.createSubscriptionCheckout(planId));
      this.success = 'Checkout preparado. Voce ja pode revisar e confirmar a assinatura.';
      await this.router.navigate(['/user/upgrade/checkout'], {
        queryParams: {
          plan: checkout.planId,
          checkoutId: checkout.checkoutId
        },
        state: {
          checkout
        }
      });
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel atualizar o plano agora.';
    } finally {
      this.switchingPlanId = null;
    }
  }

  private buildUsageRow(label: string, used: number, limit: number, suffix: string): UpgradeUsageRow {
    const cappedLimit = limit >= 9999 ? Math.max(used, 1) : Math.max(limit, 1);
    return {
      label,
      detail: limit >= 9999 ? `${used} ${suffix} / ilimitado` : `${used}/${limit} ${suffix}`,
      progress: Math.min((used / cappedLimit) * 100, 100)
    };
  }
}
