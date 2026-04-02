import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService, AccountPlanId } from 'src/app/core/services/account-plan.service';
import { SubscriptionCheckoutSession, UserService } from '../../services/user.service';

interface CheckoutPlanView {
  id: AccountPlanId;
  name: string;
  price: string;
  cadence: string;
  summary: string;
  highlights: string[];
}

@Component({
  selector: 'app-upgrade-checkout',
  templateUrl: './upgrade-checkout.page.html',
  styleUrls: ['./upgrade-checkout.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class UpgradeCheckoutPage implements OnInit {
  loading = true;
  confirming = false;
  error = '';
  success = '';
  liveCheckout = false;
  checkoutUrl = '';
  sandboxCheckoutUrl = '';
  checkoutId = '';
  selectedPlanId: AccountPlanId = 'plus';
  statusRefreshing = false;

  readonly plans: CheckoutPlanView[] = [
    {
      id: 'plus',
      name: 'Style Plus',
      price: 'R$ 1,00',
      cadence: '/mes',
      summary: 'Mais liberdade para testar estilos e manter uma rotina de looks ativa.',
      highlights: ['3 avatares ativos', '24 provas por semana', '12 fotos realistas por mes']
    },
    {
      id: 'pro',
      name: 'Style Pro',
      price: 'R$ 39,90',
      cadence: '/mes',
      summary: 'Plano equilibrado para uso frequente do provador e mais profundidade no studio.',
      highlights: ['8 avatares ativos', '60 provas por semana', 'Looks salvos ilimitados']
    },
    {
      id: 'atelier',
      name: 'Style Atelier',
      price: 'R$ 79,90',
      cadence: '/mes',
      summary: 'Camada premium para usuarios intensivos e acesso antecipado ao que vier de IA.',
      highlights: ['15 avatares ativos', '140 provas por semana', '80 fotos realistas por mes']
    }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private accountPlanService: AccountPlanService
  ) {}

  ngOnInit(): void {
    const navigationState = window.history.state as { checkout?: SubscriptionCheckoutSession };
    const checkout = navigationState?.checkout;
    const plan = (this.route.snapshot.queryParamMap.get('plan') ?? 'plus').toLowerCase();
    const checkoutId = this.route.snapshot.queryParamMap.get('checkoutId') ?? '';

    if (this.plans.some(item => item.id === plan)) {
      this.selectedPlanId = plan as AccountPlanId;
    }

    this.checkoutId = checkoutId;
    this.liveCheckout = !!checkout?.isLiveCheckout;
    this.checkoutUrl = checkout?.checkoutUrl ?? '';
    this.sandboxCheckoutUrl = checkout?.sandboxCheckoutUrl ?? '';
    this.loading = false;
  }

  get selectedPlan(): CheckoutPlanView {
    return this.plans.find(plan => plan.id === this.selectedPlanId) ?? this.plans[0];
  }

  goBack(): void {
    this.router.navigateByUrl('/user/upgrade');
  }

  async refreshPaymentStatus(): Promise<void> {
    if (this.statusRefreshing) {
      return;
    }

    this.statusRefreshing = true;
    this.error = '';
    this.success = '';

    try {
      const profile = await firstValueFrom(this.userService.getProfile());
      this.accountPlanService.hydrateFromProfile(profile);
      const paymentStatus = (profile.subscription.lastPaymentStatus || profile.subscription.status || '').toLowerCase();

      if (paymentStatus === 'approved' || paymentStatus === 'active') {
        this.success = 'Pagamento localizado. Seu plano premium ja esta ativo na conta.';
        await this.router.navigate(['/user/profile'], {
          state: {
            subscriptionActivated: true,
            activatedPlanId: this.selectedPlanId
          }
        });
        return;
      }

      this.success = 'Ainda nao apareceu confirmacao final do pagamento na conta. Tente atualizar novamente em instantes.';
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel consultar o status do pagamento agora.';
    } finally {
      this.statusRefreshing = false;
    }
  }

  async confirmSubscription(): Promise<void> {
    if (this.confirming || !this.checkoutId) {
      return;
    }

    this.confirming = true;
    this.error = '';
    this.success = '';

    try {
      const profile = await firstValueFrom(this.userService.activateSubscription(this.selectedPlanId, this.checkoutId));
      this.accountPlanService.hydrateFromProfile(profile);
      this.success = 'Assinatura ativada. O plano ja esta refletido na sua conta.';
      await this.router.navigate(['/user/profile'], {
        state: {
          subscriptionActivated: true,
          activatedPlanId: this.selectedPlanId
        }
      });
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel confirmar a assinatura agora.';
    } finally {
      this.confirming = false;
    }
  }

  openMercadoPagoCheckout(): void {
    const targetUrl = this.checkoutUrl || this.sandboxCheckoutUrl;
    if (!targetUrl) {
      this.error = 'O checkout real ainda nao esta disponivel para esta conta.';
      return;
    }

    window.location.assign(targetUrl);
  }
}
