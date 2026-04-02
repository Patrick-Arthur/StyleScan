import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ShareService } from '../core/services/share.service';
import { environment } from 'src/environments/environment';
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
  feedback = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicShowcaseService: PublicShowcaseService,
    private shareService: ShareService
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

  get publicProfileUrl(): string {
    if (!this.profile?.publicProfileSlug) {
      return environment.publicSiteUrl;
    }

    return `${environment.publicSiteUrl}/p/${this.profile.publicProfileSlug}`;
  }

  async shareProfile(): Promise<void> {
    if (!this.profile) {
      return;
    }

    const shared = await this.shareService.shareNative({
      title: `Vitrine StyleScan de ${this.profile.displayName}`,
      text: `${this.profile.displayName} publicou ${this.profile.publishedLooksCount} look(s) para inspirar, provar e comprar.`,
      url: this.publicProfileUrl
    });

    this.feedback = shared
      ? 'Link da vitrine preparado para compartilhar.'
      : 'Nao foi possivel compartilhar agora.';
  }

  async copyProfileLink(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.publicProfileUrl);
      this.feedback = 'Link da vitrine copiado.';
    } catch {
      this.feedback = 'Nao foi possivel copiar o link agora.';
    }
  }

  openStyleScan(): void {
    window.location.href = environment.publicSiteUrl;
  }
}
