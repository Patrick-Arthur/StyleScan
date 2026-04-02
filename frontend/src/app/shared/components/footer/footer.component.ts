import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-footer',
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class FooterComponent {
  readonly navItems = [
    { label: 'Home', icon: 'home-outline', route: '/home', match: '/home' },
    { label: 'Avatar', icon: 'body-outline', route: '/avatar/list', match: '/avatar' },
    { label: 'Looks', icon: 'sparkles-outline', route: '/looks/list', match: '/looks' },
    { label: 'Perfil', icon: 'person-outline', route: '/user/profile', match: '/user' }
  ];

  currentRoute = '';

  constructor(private router: Router) {
    this.currentRoute = this.router.url;

    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => {
        this.currentRoute = event.urlAfterRedirects;
      });
  }

  navigate(route: string): void {
    this.router.navigateByUrl(route);
  }

  isActive(match: string): boolean {
    return this.currentRoute.startsWith(match);
  }
}
