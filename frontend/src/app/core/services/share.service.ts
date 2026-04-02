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
  async shareNative(payload: SharePayload): Promise<boolean> {
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

  async openWhatsApp(payload: SharePayload): Promise<boolean> {
    const text = [payload.text, payload.url].filter(Boolean).join(' ');
    if (!text) {
      return false;
    }

    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank', 'noopener');
    return true;
  }

  async openFacebook(payload: SharePayload): Promise<boolean> {
    if (!payload.url) {
      return false;
    }

    window.open(`https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(payload.url)}`, '_blank', 'noopener');
    return true;
  }

  async prepareInstagram(payload: SharePayload): Promise<boolean> {
    const downloaded = await this.downloadImage(payload.imageUrl, this.buildFileName(payload.title));
    await this.copyCaption(payload);
    window.open('https://www.instagram.com/', '_blank', 'noopener');
    return downloaded;
  }

  async downloadImage(imageUrl?: string | null, fileName = 'stylescan-share'): Promise<boolean> {
    const file = await this.tryCreateFile(imageUrl, fileName);
    if (!file) {
      return false;
    }

    const objectUrl = URL.createObjectURL(file);
    const anchor = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = file.name;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    return true;
  }

  async copyLink(payload: SharePayload): Promise<boolean> {
    if (!payload.url) {
      return false;
    }

    await navigator.clipboard.writeText(payload.url);
    return true;
  }

  async copyCaption(payload: SharePayload): Promise<boolean> {
    const content = [payload.title, payload.text, payload.url].filter(Boolean).join('\n');
    await navigator.clipboard.writeText(content);
    return true;
  }

  private async tryCreateFile(imageUrl?: string | null, fileName = 'stylescan-look'): Promise<File | null> {
    if (!imageUrl) {
      return null;
    }

    const response = await fetch(imageUrl);
    if (!response.ok) {
      return null;
    }

    const blob = await response.blob();
    const extension = blob.type.includes('png') ? 'png' : blob.type.includes('webp') ? 'webp' : 'jpg';
    return new File([blob], `${fileName}.${extension}`, { type: blob.type || 'image/jpeg' });
  }

  private async copyFallback(payload: SharePayload): Promise<void> {
    const content = [payload.title, payload.text, payload.url].filter(Boolean).join('\n');
    await navigator.clipboard.writeText(content);
  }

  private buildFileName(title: string): string {
    const normalized = title
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    return normalized || 'stylescan-share';
  }
}
