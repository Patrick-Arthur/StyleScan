import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ShopListPage } from './pages/shop-list/shop-list.page';
import { ProductDetailPage } from './pages/product-detail/product-detail.page';
import { CheckoutPage } from './pages/checkout/checkout.page';

const routes: Routes = [
  {
    path: 'list',
    component: ShopListPage
  },
  {
    path: 'product/:id',
    component: ProductDetailPage
  },
  {
    path: 'checkout',
    component: CheckoutPage
  },
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ShopRoutingModule { }
