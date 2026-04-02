import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ShareService } from '../core/services/share.service';
import { environment } from 'src/environments/environment';
import { PublicLookDetail, PublicShowcaseService } from './public-showcase.service';

@Component({
  selector: 'app-public-look',
  templateUrl: './public-look.page.html',
  styleUrls: ['./public-look.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule]
})
export class PublicLookPage implements OnInit {
  detail: PublicLookDetail | null = null;
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
    void this.loadLook();
  }

  async loadLook(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.error = 'Look publico invalido.';
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      this.detail = await firstValueFrom(this.publicShowcaseService.getPublicLook(slug));
    } catch (error) {
      console.error(error);
      this.error = 'Nao foi possivel carregar esse look agora.';
    } finally {
      this.loading = false;
    }
  }

  openProfile(): void {
    if (!this.detail?.ownerPublicProfileSlug) {
      return;
    }

    this.router.navigate(['/p', this.detail.ownerPublicProfileSlug]);
  }

  openStore(url: string): void {
    if (!url) {
      return;
    }

    window.open(url, '_blank', 'noopener');
  }

  get publicLookUrl(): string {
    if (!this.detail?.look.shareSlug) {
      return environment.publicSiteUrl;
    }

    return `${environment.publicSiteUrl}/look/${this.detail.look.shareSlug}`;
  }

  async shareLook(): Promise<void> {
    if (!this.detail) {
      return;
    }

    const shared = await this.shareService.shareNative({
      title: this.detail.look.name,
      text: `${this.detail.ownerDisplayName} compartilhou um look ${this.detail.look.style} no StyleScan.`,
      url: this.publicLookUrl,
      imageUrl: this.detail.look.heroImageUrl || this.detail.items[0]?.imageUrl || null
    });

    this.feedback = shared
      ? 'Look preparado para compartilhar.'
      : 'Nao foi possivel compartilhar agora.';
  }

  async copyLookLink(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.publicLookUrl);
      this.feedback = 'Link do look copiado.';
    } catch {
      this.feedback = 'Nao foi possivel copiar o link agora.';
    }
  }

  openStyleScan(): void {
    window.location.href = environment.publicSiteUrl;
  }
}
