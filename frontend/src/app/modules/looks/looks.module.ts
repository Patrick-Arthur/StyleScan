import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IonicModule } from '@ionic/angular';

import { LooksRoutingModule } from './looks-routing.module';
import { LooksListPage } from './pages/looks-list/looks-list.page';
// import { LooksDetailPage } from './pages/looks-detail/looks-detail.page';
// import { LooksCreatePage } from './pages/looks-create/looks-create.page';
// import { LookCardComponent } from './components/look-card/look-card.component';
// import { TryOnViewerComponent } from './components/try-on-viewer/try-on-viewer.component';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    LooksRoutingModule,
    LooksListPage,
    // LooksDetailPage,
    // LooksCreatePage,
    // LookCardComponent,
    // TryOnViewerComponent
  ],
  declarations: [
  ]
})
export class LooksModule { }
