import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface LookItem {
  id: string;
  name: string;
  category: string;
  color: string;
  price: number;
  storeId: string;
  storeName: string;
  productUrl: string;
  imageUrl: string;
}

export interface LookModel {
  id: string;
  avatarId: string;
  name: string;
  occasion: string;
  style: string;
  season: string;
  note?: string | null;
  occasionTags: string[];
  heroImageUrl?: string | null;
  heroPreviewMode?: string | null;
  isPublished: boolean;
  publishedAt?: string | null;
  shareSlug?: string | null;
  totalPrice: number;
  createdAt: string;
  items: LookItem[];
}

export interface LooksListResponse {
  data: LookModel[];
  total: number;
}

export interface GenerateLookPayload {
  avatarId: string;
  occasion: string;
  style: string;
  season?: string;
  colorPreferences?: string[];
  budget?: number | null;
}

export interface SaveCustomLookPayload {
  avatarId: string;
  name: string;
  occasion: string;
  style: string;
  season?: string;
  note?: string;
  occasionTags?: string[];
  heroImageUrl?: string;
  heroPreviewMode?: string;
  productIds: string[];
}

export interface UpdateLookCoverPayload {
  heroImageUrl: string;
  heroPreviewMode?: string;
}

export interface UpdateLookDetailsPayload {
  name: string;
  note?: string;
  occasionTags?: string[];
}

export interface UpdateLookPublicationPayload {
  isPublished: boolean;
}

export interface GenerateTryOnPayload {
  avatarId: string;
  style: string;
  occasion?: string;
  boardId?: string;
  palette?: string[];
  mode?: 'avatar' | 'realistic';
  productIds: string[];
}

export interface TryOnPreviewModel {
  imageUrl: string;
  usedAi: boolean;
}

export interface TryOnPreviewHistoryModel {
  id: string;
  avatarId: string;
  style: string;
  occasion: string;
  boardId?: string | null;
  mode: 'avatar' | 'realistic';
  imageUrl: string;
  usedAi: boolean;
  productNames: string[];
  productCategories: string[];
  createdAt: string;
}

export interface TryOnPreviewHistoryListResponse {
  data: TryOnPreviewHistoryModel[];
  total: number;
}

export interface LookCollectionModel {
  id: string;
  label: string;
  looks: LookModel[];
}

export interface LookCollectionsResponse {
  data: LookCollectionModel[];
  total: number;
}

@Injectable({
  providedIn: 'root'
})
export class LooksService {
  private apiUrl = environment.apiUrl + '/looks';
  private apiOrigin = new URL(environment.apiUrl).origin;

  constructor(private http: HttpClient) { }

  generateLooks(data: GenerateLookPayload): Observable<LooksListResponse> {
    const payload: GenerateLookPayload = {
      ...data,
      colorPreferences: data.colorPreferences ?? []
    };

    return new Observable(observer => {
      this.http.post<LooksListResponse>(`${this.apiUrl}/generate`, payload).subscribe({
        next: response => {
          observer.next(this.normalizeLooksResponse(response));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  saveCustomLook(data: SaveCustomLookPayload): Observable<LookModel> {
    return new Observable(observer => {
      this.http.post<LookModel>(`${this.apiUrl}/custom`, data).subscribe({
        next: look => {
          observer.next(this.normalizeLook(look));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  updateLookCover(id: string, data: UpdateLookCoverPayload): Observable<LookModel> {
    return new Observable(observer => {
      this.http.put<LookModel>(`${this.apiUrl}/${id}/cover`, data).subscribe({
        next: look => {
          observer.next(this.normalizeLook(look));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  updateLookDetails(id: string, data: UpdateLookDetailsPayload): Observable<LookModel> {
    return new Observable(observer => {
      this.http.put<LookModel>(`${this.apiUrl}/${id}`, data).subscribe({
        next: look => {
          observer.next(this.normalizeLook(look));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  updateLookPublication(id: string, data: UpdateLookPublicationPayload): Observable<LookModel> {
    return new Observable(observer => {
      this.http.put<LookModel>(`${this.apiUrl}/${id}/publication`, data).subscribe({
        next: look => {
          observer.next(this.normalizeLook(look));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  generateTryOnPreview(data: GenerateTryOnPayload): Observable<TryOnPreviewModel> {
    return this.http.post<TryOnPreviewModel>(`${this.apiUrl}/try-on`, data);
  }

  getPreviewHistory(avatarId?: string, take = 12): Observable<TryOnPreviewHistoryListResponse> {
    const params = new URLSearchParams();
    if (avatarId) {
      params.append('avatarId', avatarId);
    }
    params.append('take', String(take));

    return this.http.get<TryOnPreviewHistoryListResponse>(`${this.apiUrl}/preview-history?${params.toString()}`);
  }

  getUserLooks(avatarId?: string, occasion?: string): Observable<LooksListResponse> {
    const params = new URLSearchParams();
    if (avatarId) {
      params.append('avatarId', avatarId);
    }
    if (occasion) {
      params.append('occasion', occasion);
    }

    const query = params.toString();
    return new Observable(observer => {
      this.http.get<LooksListResponse>(query ? `${this.apiUrl}/list?${query}` : `${this.apiUrl}/list`).subscribe({
        next: response => {
          observer.next(this.normalizeLooksResponse(response));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  getLookById(id: string): Observable<LookModel> {
    return new Observable(observer => {
      this.http.get<LookModel>(`${this.apiUrl}/${id}`).subscribe({
        next: look => {
          observer.next(this.normalizeLook(look));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  getFavoriteLooks(): Observable<LooksListResponse> {
    return new Observable(observer => {
      this.http.get<LooksListResponse>(`${this.apiUrl}/favorites`).subscribe({
        next: response => {
          observer.next(this.normalizeLooksResponse(response));
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  getLookCollections(): Observable<LookCollectionsResponse> {
    return new Observable(observer => {
      this.http.get<LookCollectionsResponse>(`${this.apiUrl}/collections`).subscribe({
        next: response => {
          observer.next({
            ...response,
            data: response.data.map(collection => ({
              ...collection,
              looks: collection.looks.map(look => this.normalizeLook(look))
            }))
          });
          observer.complete();
        },
        error: error => observer.error(error)
      });
    });
  }

  addLookToFavorites(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/favorite`, {});
  }

  removeLookFromFavorites(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}/favorite`);
  }

  addLookToCollection(id: string, name: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/collections`, { name });
  }

  removeLookFromCollection(id: string, collectionId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}/collections/${encodeURIComponent(collectionId)}`);
  }

  renameLookCollection(collectionId: string, name: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/collections/${encodeURIComponent(collectionId)}`, { name });
  }

  deleteLookCollection(collectionId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/collections/${encodeURIComponent(collectionId)}`);
  }

  private normalizeLooksResponse(response: LooksListResponse): LooksListResponse {
    return {
      ...response,
      data: response.data.map(look => this.normalizeLook(look))
    };
  }

  private normalizeLook(look: LookModel): LookModel {
    return {
      ...look,
      occasionTags: look.occasionTags ?? [],
      heroImageUrl: this.resolveAbsoluteUrl(look.heroImageUrl),
      isPublished: !!look.isPublished
    };
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
