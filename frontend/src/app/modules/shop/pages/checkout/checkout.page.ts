import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { CurrencyPipe } from 'src/app/shared/pipes/currency.pipe';
import { CartItem, CartService, ShippingAddress } from '../../services/cart.service';

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.page.html',
  styleUrls: ['./checkout.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule, CurrencyPipe]
})
export class CheckoutPage implements OnInit {
  cartItems: CartItem[] = [];
  totalAmount = 0;
  processing = false;
  shippingAddress: ShippingAddress = {
    street: '',
    city: '',
    state: '',
    zipCode: '',
    country: 'Brasil'
  };
  error = '';

  constructor(private cartService: CartService, private router: Router) {}

  ngOnInit(): void {
    this.loadCart();
  }

  ionViewWillEnter(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.cartItems = this.cartService.getItems();
    this.calculateTotal();
    this.error = '';
  }

  calculateTotal(): void {
    this.totalAmount = this.cartItems.reduce((acc, item) => acc + (item.price * item.quantity), 0);
  }

  updateQuantity(itemId: string, quantity: number): void {
    this.cartService.updateQuantity(itemId, quantity);
    this.loadCart();
  }

  removeItem(itemId: string): void {
    this.cartService.removeItem(itemId);
    this.loadCart();
  }

  async processCheckout(): Promise<void> {
    if (this.processing) {
      return;
    }

    if (!this.cartItems.length) {
      this.error = 'Seu carrinho esta vazio.';
      return;
    }

    if (!this.shippingAddress.street || !this.shippingAddress.city || !this.shippingAddress.state || !this.shippingAddress.zipCode) {
      this.error = 'Preencha o endereco completo para finalizar.';
      return;
    }

    this.error = '';
    this.processing = true;

    try {
      const order = this.cartService.placeOrder(this.shippingAddress);
      await this.router.navigate(['/user/history'], {
        state: { recentOrderId: order.id }
      });
    } finally {
      this.processing = false;
    }
  }
}
