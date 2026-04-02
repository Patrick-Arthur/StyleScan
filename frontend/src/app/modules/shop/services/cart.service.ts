import { Injectable } from '@angular/core';
import { ProductModel } from './shop.service';

const CART_STORAGE_KEY = 'stylescan.cartItems';
const ORDER_STORAGE_KEY = 'stylescan.orderHistory';

export interface CartItem {
  id: string;
  name: string;
  price: number;
  quantity: number;
  imageUrl: string;
  size: string;
}

export interface ShippingAddress {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface OrderRecord {
  id: string;
  date: string;
  total: number;
  items: number;
  status: string;
  previewImageUrl?: string;
  shippingAddress: ShippingAddress;
}

@Injectable({
  providedIn: 'root'
})
export class CartService {
  getItems(): CartItem[] {
    return this.readCart();
  }

  addProduct(product: ProductModel, size: string = 'M'): void {
    const items = this.readCart();
    const existing = items.find(item => item.id === product.id && item.size === size);

    if (existing) {
      existing.quantity += 1;
    } else {
      items.push({
        id: product.id,
        name: product.name,
        price: product.price,
        quantity: 1,
        imageUrl: product.imageUrl,
        size
      });
    }

    this.writeCart(items);
  }

  updateQuantity(itemId: string, quantity: number): void {
    const nextItems = this.readCart()
      .map(item => item.id === itemId ? { ...item, quantity } : item)
      .filter(item => item.quantity > 0);

    this.writeCart(nextItems);
  }

  removeItem(itemId: string): void {
    this.writeCart(this.readCart().filter(item => item.id !== itemId));
  }

  clearCart(): void {
    localStorage.removeItem(CART_STORAGE_KEY);
  }

  getTotalAmount(): number {
    return this.readCart().reduce((total, item) => total + (item.price * item.quantity), 0);
  }

  placeOrder(shippingAddress: ShippingAddress): OrderRecord {
    const items = this.readCart();
    const order: OrderRecord = {
      id: `ord-${Date.now()}`,
      date: new Date().toISOString(),
      total: items.reduce((total, item) => total + (item.price * item.quantity), 0),
      items: items.reduce((total, item) => total + item.quantity, 0),
      status: 'Processando',
      previewImageUrl: items[0]?.imageUrl,
      shippingAddress
    };

    const history = this.readOrders();
    history.unshift(order);
    this.writeOrders(history);
    this.clearCart();

    return order;
  }

  getOrderHistory(): OrderRecord[] {
    return this.readOrders();
  }

  private readCart(): CartItem[] {
    const raw = localStorage.getItem(CART_STORAGE_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as CartItem[];
    } catch {
      return [];
    }
  }

  private writeCart(items: CartItem[]): void {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
  }

  private readOrders(): OrderRecord[] {
    const raw = localStorage.getItem(ORDER_STORAGE_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as OrderRecord[];
    } catch {
      return [];
    }
  }

  private writeOrders(orders: OrderRecord[]): void {
    localStorage.setItem(ORDER_STORAGE_KEY, JSON.stringify(orders));
  }
}
