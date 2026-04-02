import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';
import { LandingPageRoutingModule } from './landing-routing.module';
import { LandingPage } from './landing.page';
import { PrivacyPage } from './privacy.page';
import { TermsPage } from './terms.page';
import { PublicProfilePage } from './public-profile.page';
import { PublicLookPage } from './public-look.page';

@NgModule({
  imports: [
    CommonModule,
    IonicModule,
    LandingPageRoutingModule,
    PublicProfilePage,
    PublicLookPage
  ],
  declarations: [LandingPage, PrivacyPage, TermsPage]
})
export class LandingPageModule {}
