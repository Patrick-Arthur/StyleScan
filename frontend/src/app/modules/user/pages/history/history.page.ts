import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { Navigation, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { CartService, OrderRecord } from 'src/app/modules/shop/services/cart.service';
import { CurrencyPipe } from 'src/app/shared/pipes/currency.pipe';

@Component({
  selector: 'app-history',
  templateUrl: './history.page.html',
  styleUrls: ['./history.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, CurrencyPipe]
})
export class HistoryPage {
  orderHistory: OrderRecord[] = [];
  recentOrderId = '';

  constructor(
    private cartService: CartService,
    private router: Router
  ) {}

  ionViewWillEnter(): void {
    this.orderHistory = this.cartService.getOrderHistory();
    const navigation: Navigation | null = this.router.getCurrentNavigation();
    this.recentOrderId = navigation?.extras?.state?.['recentOrderId'] ?? history.state?.recentOrderId ?? '';
  }

  isRecentOrder(orderId: string): boolean {
    return this.recentOrderId === orderId;
  }
}
