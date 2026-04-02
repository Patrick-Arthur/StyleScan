import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService } from 'src/app/core/services/account-plan.service';
import { AvatarService } from '../../services/avatar.service';

@Component({
  selector: 'app-avatar-create',
  templateUrl: './avatar-create.page.html',
  styleUrls: ['./avatar-create.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class AvatarCreatePage implements OnInit {
  avatarForm: FormGroup = new FormGroup({});
  submitting = false;
  error = '';
  planHint = '';
  selectedPhotos: File[] = [];
  selectedPhotoPreviews: string[] = [];

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private avatarService: AvatarService,
    private accountPlanService: AccountPlanService
  ) {}

  ngOnInit(): void {
    this.avatarForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      gender: ['female', Validators.required],
      bodyType: ['average', Validators.required],
      skinTone: ['medium', Validators.required],
      height: [170, [Validators.required, Validators.min(120), Validators.max(230)]],
      chest: [90, [Validators.required, Validators.min(50), Validators.max(180)]],
      waist: [75, [Validators.required, Validators.min(40), Validators.max(180)]],
      hips: [95, [Validators.required, Validators.min(50), Validators.max(200)]]
    });

    void this.loadPlanHint();
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedPhotos = Array.from(input.files ?? []);
    this.selectedPhotoPreviews = this.selectedPhotos.map(photo => URL.createObjectURL(photo));
  }

  async createAvatar(): Promise<void> {
    if (this.avatarForm.invalid) {
      this.avatarForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.error = '';

    try {
      await firstValueFrom(this.accountPlanService.loadRemoteState());
      const avatarResponse = await firstValueFrom(this.avatarService.getUserAvatars());
      const limitCheck = this.accountPlanService.canCreateAvatar(avatarResponse.total);
      if (!limitCheck.allowed) {
        this.error = `${limitCheck.reason} ${this.accountPlanService.getUpgradePrompt('avatarSlots')}`;
        return;
      }

      const avatar = await firstValueFrom(this.avatarService.createAvatar({
        ...this.avatarForm.getRawValue(),
        photos: this.selectedPhotos
      }));

      await this.router.navigateByUrl(`/avatar/customize/${avatar.id}`, { replaceUrl: true });
    } catch (error) {
      this.error = 'Nao foi possivel criar o avatar agora.';
      console.error(error);
    } finally {
      this.submitting = false;
    }
  }

  goToUpgrade(): void {
    this.router.navigateByUrl('/user/upgrade');
  }

  private async loadPlanHint(): Promise<void> {
    try {
      await firstValueFrom(this.accountPlanService.loadRemoteState());
      const avatarResponse = await firstValueFrom(this.avatarService.getUserAvatars());
      const limit = this.accountPlanService.getCurrentPlan().limits.avatars;
      const remaining = Math.max(limit - avatarResponse.total, 0);

      if (remaining <= 0) {
        this.planHint = 'Seu plano atual nao tem mais slots para novos avatares. Um upgrade libera novas bases visuais para testes.';
        return;
      }

      if (remaining === 1) {
        this.planHint = 'Este avatar vai ocupar o ultimo slot disponivel do seu plano atual.';
      }
    } catch {
      this.planHint = '';
    }
  }
}
