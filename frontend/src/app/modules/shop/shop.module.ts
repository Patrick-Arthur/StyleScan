import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IonicModule } from '@ionic/angular';

import { ShopRoutingModule } from './shop-routing.module';
import { ShopListPage } from './pages/shop-list/shop-list.page';
import { ProductDetailPage } from './pages/product-detail/product-detail.page';
import { CheckoutPage } from './pages/checkout/checkout.page';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    ShopRoutingModule,
    ShopListPage,
    ProductDetailPage,
    CheckoutPage
  ],
  declarations: [  ]
})
export class ShopModule { }
