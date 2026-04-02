import { Injectable } from '@angular/core';

export interface SharePayload {
  title: string;
  text: string;
  url?: string;
  imageUrl?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ShareService {
  async share(payload: SharePayload): Promise<boolean> {
    const nav = navigator as Navigator & {
      canShare?: (data?: ShareData) => boolean;
    };

    try {
      const file = await this.tryCreateFile(payload.imageUrl);
      if (file && nav.share && nav.canShare?.({ files: [file] })) {
        await nav.share({
          title: payload.title,
          text: payload.text,
          url: payload.url,
          files: [file]
        });
        return true;
      }

      if (nav.share) {
        await nav.share({
          title: payload.title,
          text: payload.text,
          url: payload.url
        });
        return true;
      }

      await this.copyFallback(payload);
      return true;
    } catch {
      try {
        await this.copyFallback(payload);
        return true;
      } catch {
        return false;
      }
    }
  }

  private async tryCreateFile(imageUrl?: string | null): Promise<File | null> {
    if (!imageUrl) {
      return null;
    }

    const response = await fetch(imageUrl);
    if (!response.ok) {
      return null;
    }

    const blob = await response.blob();
    const extension = blob.type.includes('png') ? 'png' : blob.type.includes('webp') ? 'webp' : 'jpg';
    return new File([blob], `stylescan-look.${extension}`, { type: blob.type || 'image/jpeg' });
  }

  private async copyFallback(payload: SharePayload): Promise<void> {
    const content = [payload.title, payload.text, payload.url].filter(Boolean).join('\n');
    await navigator.clipboard.writeText(content);
  }
}
