import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AvatarModel, AvatarService } from '../../services/avatar.service';

@Component({
  selector: 'app-avatar-customize',
  templateUrl: './avatar-customize.page.html',
  styleUrls: ['./avatar-customize.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class AvatarCustomizePage implements OnInit {
  avatarId: string | null = '';
  avatar: AvatarModel | null = null;
  avatarForm: FormGroup = new FormGroup({});
  loading = true;
  saving = false;
  updatingPhoto = false;
  generatingAvatar2d = false;
  error = '';
  photoPreview = '';
  photoGallery: string[] = [];
  brokenPhotoGalleryUrls = new Set<string>();

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private avatarService: AvatarService
  ) {}

  ngOnInit() {
    this.avatarForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      gender: ['', Validators.required],
      bodyType: ['', Validators.required],
      skinTone: ['', Validators.required],
      height: [0, [Validators.required, Validators.min(120), Validators.max(230)]],
      weight: [0, [Validators.required, Validators.min(35), Validators.max(250)]],
      chest: [0, [Validators.required, Validators.min(50), Validators.max(180)]],
      waist: [0, [Validators.required, Validators.min(40), Validators.max(180)]],
      hips: [0, [Validators.required, Validators.min(50), Validators.max(200)]]
    });

    this.avatarId = this.route.snapshot.paramMap.get('id');
    this.loadAvatar();
  }

  async loadAvatar(): Promise<void> {
    if (!this.avatarId) {
      this.error = 'Avatar invalido.';
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      this.avatar = await firstValueFrom(this.avatarService.getAvatarById(this.avatarId));
      this.syncAvatarState();
    } catch (error) {
      this.error = 'Nao foi possivel carregar esse avatar.';
      console.error(error);
    } finally {
      this.loading = false;
    }
  }

  async saveCustomization(): Promise<void> {
    if (!this.avatarId || this.avatarForm.invalid) {
      this.avatarForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = '';

    try {
      this.avatar = await firstValueFrom(this.avatarService.updateAvatar(this.avatarId, {
        name: this.avatarForm.value.name,
        gender: this.avatarForm.value.gender,
        bodyType: this.avatarForm.value.bodyType,
        skinTone: this.avatarForm.value.skinTone,
        measurements: {
          height: this.avatarForm.value.height,
          weight: this.avatarForm.value.weight,
          chest: this.avatarForm.value.chest,
          waist: this.avatarForm.value.waist,
          hips: this.avatarForm.value.hips
        }
      }));
    } catch (error) {
      this.error = 'Nao foi possivel salvar as alteracoes.';
      console.error(error);
    } finally {
      this.saving = false;
    }
  }

  resetCustomization(): void {
    if (!this.avatar) {
      return;
    }

    this.avatarForm.reset({
      name: this.avatar.name,
      gender: this.avatar.gender,
      bodyType: this.avatar.bodyType,
      skinTone: this.avatar.skinTone,
      height: this.avatar.height,
      weight: this.avatar.weight,
      chest: this.avatar.chest,
      waist: this.avatar.waist,
      hips: this.avatar.hips
    });
  }

  async onPhotoSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const photos = Array.from(input.files ?? []);
    if (!photos.length || !this.avatarId) {
      return;
    }

    this.updatingPhoto = true;
    this.error = '';

    try {
      this.avatar = await firstValueFrom(this.avatarService.updateAvatarPhotos(this.avatarId, photos));
      this.syncAvatarState();
    } catch (error) {
      this.error = 'Nao foi possivel atualizar as fotos-base do avatar.';
      console.error(error);
    } finally {
      this.updatingPhoto = false;
      input.value = '';
    }
  }

  getAvatarImageUrl(): string {
    if (this.photoPreview) {
      return this.photoPreview;
    }

    return this.avatarService.resolveAvatarImageUrl(this.avatar);
  }

  get visiblePhotoGallery(): string[] {
    return this.photoGallery.filter(photo => !this.brokenPhotoGalleryUrls.has(photo));
  }

  get avatarVisualLabel(): string {
    return this.avatarService.resolveAvatarVisualLabel(this.avatar);
  }

  onGalleryImageError(photoUrl: string): void {
    this.brokenPhotoGalleryUrls.add(photoUrl);
    this.brokenPhotoGalleryUrls = new Set(this.brokenPhotoGalleryUrls);
  }

  async generateAvatar2d(): Promise<void> {
    if (!this.avatarId) {
      return;
    }

    this.generatingAvatar2d = true;
    this.error = '';

    try {
      this.avatar = await firstValueFrom(this.avatarService.generateTwoDimensionalAvatar(this.avatarId));
      this.syncAvatarState();
    } catch (error) {
      this.error = 'Nao foi possivel gerar o avatar 2D agora.';
      console.error(error);
    } finally {
      this.generatingAvatar2d = false;
    }
  }

  private syncAvatarState(): void {
    if (!this.avatar) {
      return;
    }

    this.photoPreview = this.avatarService.resolveAvatarImageUrl(this.avatar);
    this.photoGallery = this.avatarService.resolveAvatarGallery(this.avatar);
    this.brokenPhotoGalleryUrls = new Set();
    this.avatarForm.patchValue({
      name: this.avatar.name,
      gender: this.avatar.gender,
      bodyType: this.avatar.bodyType,
      skinTone: this.avatar.skinTone,
      height: this.avatar.height,
      weight: this.avatar.weight,
      chest: this.avatar.chest,
      waist: this.avatar.waist,
      hips: this.avatar.hips
    });
  }
}
