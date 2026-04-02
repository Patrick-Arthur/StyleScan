import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '@ionic/angular';

// Components
import { HeaderComponent } from './components/header/header.component';
import { FooterComponent } from './components/footer/footer.component';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';

// Pipes
import { CurrencyPipe } from './pipes/currency.pipe';

// Directives
// import { SomeDirective } from './directives/some.directive';

@NgModule({
  declarations: [
    // SomeDirective
  ],
  imports: [
    CommonModule,
    IonicModule,
    HeaderComponent,
    FooterComponent,
    LoadingSpinnerComponent,
    CurrencyPipe
  ],
  exports: [
    CurrencyPipe,
    // SomeDirective
    IonicModule, // Export IonicModule for convenience
    HeaderComponent,
    FooterComponent,
    LoadingSpinnerComponent
  ]
})
export class SharedModule { }
