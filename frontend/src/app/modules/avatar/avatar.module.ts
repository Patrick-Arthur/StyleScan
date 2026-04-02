import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { IonicModule } from '@ionic/angular';

import { AvatarRoutingModule } from './avatar-routing.module';
import { AvatarCreatePage } from './pages/avatar-create/avatar-create.page';
import { AvatarCustomizePage } from './pages/avatar-customize/avatar-customize.page';
import { AvatarListPage } from './pages/avatar-list/avatar-list.page';
// import { AvatarViewerComponent } from './components/avatar-viewer/avatar-viewer.component';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    IonicModule,
    AvatarRoutingModule,
    AvatarCreatePage,
    AvatarCustomizePage,
    AvatarListPage,
  ],
  declarations: [
    // AvatarViewerComponent
  ]
})
export class AvatarModule { }
