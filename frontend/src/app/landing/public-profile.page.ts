import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { PublicProfile, PublicShowcaseService } from './public-showcase.service';

@Component({
  selector: 'app-public-profile',
  templateUrl: './public-profile.page.html',
  styleUrls: ['./public-profile.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class PublicProfilePage implements OnInit {
  profile: PublicProfile | null = null;
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicShowcaseService: PublicShowcaseService
  ) {}

  ngOnInit(): void {
    void this.loadProfile();
  }

  async loadProfile(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.error = 'Perfil publico invalido.';
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      this.profile = await firstValueFrom(this.publicShowcaseService.getPublicProfile(slug));
    } catch (error) {
      console.error(error);
      this.error = 'Nao foi possivel carregar essa vitrine agora.';
    } finally {
      this.loading = false;
    }
  }

  openLook(shareSlug: string): void {
    this.router.navigate(['/look', shareSlug]);
  }
}
