import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface AvatarMeasurements {
  height: number;
  weight: number;
  chest: number;
  waist: number;
  hips: number;
}

export interface AvatarModel extends AvatarMeasurements {
  id: string;
  userId: string;
  name: string;
  modelUrl: string;
  photoUrl?: string | null;
  photoUrls: string[];
  generatedAvatarImageUrl?: string | null;
  gender: string;
  bodyType: string;
  skinTone: string;
  createdAt: string;
  updatedAt: string;
}

export interface AvatarListResponse {
  data: AvatarModel[];
  total: number;
}

export interface CreateAvatarPayload extends AvatarMeasurements {
  name: string;
  gender: string;
  bodyType: string;
  skinTone: string;
  photos: File[];
}

export interface UpdateAvatarPayload {
  name?: string;
  gender?: string;
  bodyType?: string;
  skinTone?: string;
  measurements?: AvatarMeasurements;
}

@Injectable({
  providedIn: 'root'
})
export class AvatarService {
  private apiUrl = environment.apiUrl + '/avatar';
  private apiOrigin = new URL(environment.apiUrl).origin;

  constructor(private http: HttpClient) { }

  createAvatar(payload: CreateAvatarPayload): Observable<AvatarModel> {
    const formData = new FormData();
    formData.append('name', payload.name);
    formData.append('gender', payload.gender);
    formData.append('bodyType', payload.bodyType);
    formData.append('skinTone', payload.skinTone);
    formData.append('height', String(payload.height));
    formData.append('weight', String(payload.weight));
    formData.append('chest', String(payload.chest));
    formData.append('waist', String(payload.waist));
    formData.append('hips', String(payload.hips));

    for (const photo of payload.photos) {
      formData.append('photos', photo);
    }

    return this.http.post<AvatarModel>(`${this.apiUrl}/create`, formData);
  }

  getUserAvatars(): Observable<AvatarListResponse> {
    return this.http.get<AvatarListResponse>(`${this.apiUrl}/list`);
  }

  getAvatarById(id: string): Observable<AvatarModel> {
    return this.http.get<AvatarModel>(`${this.apiUrl}/${id}`);
  }

  updateAvatar(id: string, data: UpdateAvatarPayload): Observable<AvatarModel> {
    return this.http.put<AvatarModel>(`${this.apiUrl}/${id}`, data);
  }

  updateAvatarPhotos(id: string, photos: File[]): Observable<AvatarModel> {
    const formData = new FormData();
    for (const photo of photos) {
      formData.append('photos', photo);
    }
    return this.http.put<AvatarModel>(`${this.apiUrl}/${id}/photos`, formData);
  }

  generateTwoDimensionalAvatar(id: string): Observable<AvatarModel> {
    return this.http.post<AvatarModel>(`${this.apiUrl}/${id}/generate-2d`, {});
  }

  resolveAbsoluteUrl(source: string): string {
    if (!source) {
      return '';
    }

    if (source.startsWith('http://') || source.startsWith('https://')) {
      return source;
    }

    return `${this.apiOrigin}${source}`;
  }

  resolveAvatarImageUrl(avatar: Pick<AvatarModel, 'generatedAvatarImageUrl' | 'photoUrl' | 'photoUrls' | 'modelUrl' | 'updatedAt'> | null | undefined): string {
    const source = avatar?.generatedAvatarImageUrl || avatar?.photoUrl || avatar?.photoUrls?.[0] || avatar?.modelUrl || '';
    if (!source) {
      return '';
    }

    return this.withVersion(this.resolveAbsoluteUrl(source), avatar);
  }

  resolveAvatarGallery(avatar: Pick<AvatarModel, 'photoUrls' | 'photoUrl' | 'updatedAt'> | null | undefined): string[] {
    const sources = avatar?.photoUrls?.length ? avatar.photoUrls : (avatar?.photoUrl ? [avatar.photoUrl] : []);
    return sources.map(source => this.withVersion(this.resolveAbsoluteUrl(source), avatar));
  }

  private withVersion(url: string, avatar: Pick<AvatarModel, 'updatedAt'> | null | undefined): string {
    const version = avatar?.updatedAt ? new Date(avatar.updatedAt).getTime() : Date.now();
    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}v=${version}`;
  }

  deleteAvatar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
