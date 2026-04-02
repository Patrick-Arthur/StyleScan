import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { AvatarCreatePage } from './pages/avatar-create/avatar-create.page';
import { AvatarCustomizePage } from './pages/avatar-customize/avatar-customize.page';
import { AvatarListPage } from './pages/avatar-list/avatar-list.page';

const routes: Routes = [
  {
    path: 'create',
    component: AvatarCreatePage
  },
  {
    path: 'customize/:id',
    component: AvatarCustomizePage
  },
  {
    path: 'list',
    component: AvatarListPage
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
export class AvatarRoutingModule { }
