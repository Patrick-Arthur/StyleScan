import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  standalone: false,
})
export class AppComponent {
  private readonly footerHiddenRoutes = ['/auth', '/landing', '/privacy', '/terms'];

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  get showBottomNav(): boolean {
    const currentUrl = this.router.url;
    const isRootPublicRoute = currentUrl === '' || currentUrl === '/';
    const isHiddenRoute = isRootPublicRoute || this.footerHiddenRoutes.some(route => currentUrl.startsWith(route));

    return this.authService.isAuthenticated() && !isHiddenRoute;
  }
}
