import { Injectable } from '@angular/core';
import { LookModel } from './looks.service';

const STORAGE_KEY = 'stylescan.favoriteLooks';

@Injectable({
  providedIn: 'root'
})
export class LookFavoritesService {
  getFavorites(): LookModel[] {
    return this.read();
  }

  isFavorite(lookId: string): boolean {
    return this.read().some(look => look.id === lookId);
  }

  toggleFavorite(look: LookModel): boolean {
    const favorites = this.read();
    const existingIndex = favorites.findIndex(item => item.id === look.id);

    if (existingIndex >= 0) {
      favorites.splice(existingIndex, 1);
      this.write(favorites);
      return false;
    }

    favorites.unshift(look);
    this.write(favorites);
    return true;
  }

  upsertFavorite(look: LookModel): void {
    const favorites = this.read().filter(item => item.id !== look.id);
    favorites.unshift(look);
    this.write(favorites);
  }

  removeFavorite(lookId: string): void {
    this.write(this.read().filter(look => look.id !== lookId));
  }

  private read(): LookModel[] {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as LookModel[];
    } catch {
      return [];
    }
  }

  private write(favorites: LookModel[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(favorites));
  }
}
