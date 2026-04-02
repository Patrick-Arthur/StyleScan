import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LookItem } from '../modules/looks/services/looks.service';

export interface PublicLookSummary {
  id: string;
  name: string;
  occasion: string;
  style: string;
  note?: string | null;
  occasionTags: string[];
  heroImageUrl?: string | null;
  heroPreviewMode?: string | null;
  shareSlug: string;
  totalPrice: number;
  createdAt: string;
  publishedAt?: string | null;
}

export interface PublicProfile {
  displayName: string;
  publicProfileSlug: string;
  bio?: string | null;
  publishedLooksCount: number;
  looks: PublicLookSummary[];
}

export interface PublicLookDetail {
  ownerDisplayName: string;
  ownerPublicProfileSlug: string;
  look: PublicLookSummary;
  items: LookItem[];
}

@Injectable({
  providedIn: 'root'
})
export class PublicShowcaseService {
  private readonly apiUrl = `${environment.apiUrl}/public`;
  private readonly apiOrigin = new URL(environment.apiUrl).origin;

  constructor(private http: HttpClient) {}

  getPublicProfile(slug: string): Observable<PublicProfile> {
    return new Observable(observer => {
      this.http.get<PublicProfile>(`${this.apiUrl}/profiles/${encodeURIComponent(slug)}`).subscribe({
        next: profile => {
          observer.next({
            ...profile,
            looks: profile.looks.map(look => ({
              ...look,
              heroImageUrl: this.resolveAbsoluteUrl(look.heroImageUrl)
            }))
          });
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  getPublicLook(slug: string): Observable<PublicLookDetail> {
    return new Observable(observer => {
      this.http.get<PublicLookDetail>(`${this.apiUrl}/looks/${encodeURIComponent(slug)}`).subscribe({
        next: detail => {
          observer.next({
            ...detail,
            look: {
              ...detail.look,
              heroImageUrl: this.resolveAbsoluteUrl(detail.look.heroImageUrl)
            },
            items: detail.items.map(item => ({
              ...item,
              imageUrl: this.resolveAbsoluteUrl(item.imageUrl) ?? item.imageUrl
            }))
          });
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  private resolveAbsoluteUrl(source?: string | null): string | null {
    if (!source) {
      return null;
    }

    if (source.startsWith('http://') || source.startsWith('https://')) {
      return source;
    }

    return `${this.apiOrigin}${source}`;
  }
}
