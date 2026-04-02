import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LandingPage } from './landing.page';
import { PublicLookPage } from './public-look.page';
import { PublicProfilePage } from './public-profile.page';
import { PrivacyPage } from './privacy.page';
import { TermsPage } from './terms.page';

const routes: Routes = [
  {
    path: '',
    component: LandingPage
  },
  {
    path: 'privacy',
    component: PrivacyPage
  },
  {
    path: 'p/:slug',
    component: PublicProfilePage
  },
  {
    path: 'look/:slug',
    component: PublicLookPage
  },
  {
    path: 'terms',
    component: TermsPage
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class LandingPageRoutingModule {}
