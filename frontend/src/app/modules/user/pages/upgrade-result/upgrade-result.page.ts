import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService, AccountPlanId } from 'src/app/core/services/account-plan.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-upgrade-result',
  templateUrl: './upgrade-result.page.html',
  styleUrls: ['./upgrade-result.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class UpgradeResultPage implements OnInit {
  loading = false;
  confirming = false;
  status = 'pending';
  planId: AccountPlanId = 'plus';
  checkoutId = '';
  error = '';
  success = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private accountPlanService: AccountPlanService
  ) {}

  ngOnInit(): void {
    const status = (this.route.snapshot.queryParamMap.get('status') ?? this.route.snapshot.queryParamMap.get('collection_status') ?? 'pending').toLowerCase();
    const plan = (this.route.snapshot.queryParamMap.get('plan') ?? 'plus').toLowerCase();
    const checkoutId = this.route.snapshot.queryParamMap.get('checkoutId') ?? this.route.snapshot.queryParamMap.get('external_reference') ?? '';

    this.status = status;
    if (['plus', 'pro', 'atelier'].includes(plan)) {
      this.planId = plan as AccountPlanId;
    }
    this.checkoutId = checkoutId;

    if (this.status === 'approved' && this.checkoutId) {
      void this.confirmFromReturn();
    }
  }

  get title(): string {
    if (this.status === 'approved') {
      return 'Pagamento aprovado';
    }

    if (this.status === 'rejected' || this.status === 'failure') {
      return 'Pagamento nao concluido';
    }

    return 'Pagamento em analise';
  }

  get description(): string {
    if (this.status === 'approved') {
      return 'Estamos finalizando a ativacao do seu plano dentro do StyleScan.';
    }

    if (this.status === 'rejected' || this.status === 'failure') {
      return 'Voce pode voltar aos planos e tentar novamente quando quiser.';
    }

    return 'O Mercado Pago ainda esta processando esta transacao. Voce pode acompanhar e retornar depois.';
  }

  async confirmFromReturn(): Promise<void> {
    if (this.confirming) {
      return;
    }

    this.confirming = true;
    this.error = '';
    this.success = '';

    try {
      const profile = await firstValueFrom(this.userService.activateSubscription(this.planId, this.checkoutId));
      this.accountPlanService.hydrateFromProfile(profile);
      this.success = 'Plano ativado com sucesso na sua conta.';
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.error = httpError.error?.message || 'Nao foi possivel concluir a ativacao do plano.';
    } finally {
      this.confirming = false;
    }
  }

  goToProfile(): void {
    this.router.navigateByUrl('/user/profile');
  }

  goToUpgrade(): void {
    this.router.navigateByUrl('/user/upgrade');
  }
}
