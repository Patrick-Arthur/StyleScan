import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { ProfilePage } from './pages/profile/profile.page';
import { FavoritesPage } from './pages/favorites/favorites.page';
import { HistoryPage } from './pages/history/history.page';
import { UpgradePage } from './pages/upgrade/upgrade.page';
import { UpgradeCheckoutPage } from './pages/upgrade-checkout/upgrade-checkout.page';
import { UpgradeResultPage } from './pages/upgrade-result/upgrade-result.page';

const routes: Routes = [
  {
    path: 'profile',
    component: ProfilePage
  },
  {
    path: 'favorites',
    component: FavoritesPage
  },
  {
    path: 'history',
    component: HistoryPage
  },
  {
    path: 'upgrade',
    component: UpgradePage
  },
  {
    path: 'upgrade/checkout',
    component: UpgradeCheckoutPage
  },
  {
    path: 'upgrade/result',
    component: UpgradeResultPage
  },
  {
    path: '',
    redirectTo: 'profile',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class UserRoutingModule { }
