import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IonicModule } from '@ionic/angular';

import { UserRoutingModule } from './user-routing.module';
import { ProfilePage } from './pages/profile/profile.page';
import { FavoritesPage } from './pages/favorites/favorites.page';
import { HistoryPage } from './pages/history/history.page';
import { UpgradePage } from './pages/upgrade/upgrade.page';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    UserRoutingModule,
    HistoryPage,
    ProfilePage,
    FavoritesPage,
    UpgradePage
  ],
  declarations: [
  ]
})
export class UserModule { }
