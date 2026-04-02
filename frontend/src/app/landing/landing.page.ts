import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-landing',
  templateUrl: './landing.page.html',
  styleUrls: ['./landing.page.scss'],
  standalone: false
})
export class LandingPage implements OnInit {
  paymentStatus = '';

  readonly highlights = [
    'Curadoria de produtos de moda, calcados e acessorios por estilo e contexto',
    'Perfil visual com fotos de referencia, avatar 2D e personalizacao assistida por IA',
    'Montagem de looks com provador digital e exploracao guiada por ocasiao',
    'Redirecionamento para paginas de compra de parceiros e varejistas externos'
  ];

  readonly steps = [
    {
      title: 'Perfil e referencia visual',
      description: 'O usuario informa medidas, preferencias e fotos para estruturar uma experiencia de moda mais personalizada.'
    },
    {
      title: 'Descoberta e curadoria',
      description: 'O aplicativo organiza estilos, produtos e combinacoes para facilitar a exploracao e a comparacao visual.'
    },
    {
      title: 'Decisao e compra externa',
      description: 'O usuario testa looks, salva favoritos e segue para a compra em lojas e parceiros externos.'
    }
  ];

  readonly trustPoints = [
    'Aplicativo mobile-first focado em descoberta de moda e experiencia guiada por estilo',
    'Uso de IA para apoiar avatar, provador visual e personalizacao do acervo',
    'Fluxo de compra realizado em parceiros externos e ambientes de pagamento reconhecidos',
    'Dados de perfil e preferencias usados para personalizacao da experiencia dentro do produto'
  ];

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const status = (params.get('mpStatus') ?? '').toLowerCase();
      this.paymentStatus = ['success', 'pending', 'failure'].includes(status) ? status : '';
    });
  }

  get paymentMessage(): string {
    switch (this.paymentStatus) {
      case 'success':
        return 'Pagamento autorizado no Mercado Pago. Se a assinatura foi iniciada no app, volte ao StyleScan para continuar a ativacao.';
      case 'pending':
        return 'Seu pagamento ficou pendente de confirmacao no Mercado Pago. Voce pode acompanhar o status e depois voltar ao StyleScan.';
      case 'failure':
        return 'O pagamento nao foi concluido. Voce pode tentar novamente pelo app quando quiser.';
      default:
        return '';
    }
  }

  goToLogin(): void {
    this.router.navigateByUrl('/auth/login');
  }

  goToRegister(): void {
    this.router.navigateByUrl('/auth/register');
  }
}
