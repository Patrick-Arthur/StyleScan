import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LooksDetailPage } from './pages/looks-detail/looks-detail.page';
import { LooksListPage } from './pages/looks-list/looks-list.page';

const routes: Routes = [
  {
    path: 'list',
    component: LooksListPage
  },
  {
    path: ':id',
    component: LooksDetailPage
  },
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class LooksRoutingModule {}
