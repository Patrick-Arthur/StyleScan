import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
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

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private publicShowcaseService: PublicShowcaseService
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
}
