import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-register',
  templateUrl: './register.page.html',
  styleUrls: ['./register.page.scss'],
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class RegisterPage implements OnInit {
  registerForm: FormGroup = new FormGroup({});
  registerError = '';
  readonly dayOptions = Array.from({ length: 31 }, (_, index) => index + 1);
  readonly monthOptions = [
    { value: 1, label: 'Janeiro' },
    { value: 2, label: 'Fevereiro' },
    { value: 3, label: 'Marco' },
    { value: 4, label: 'Abril' },
    { value: 5, label: 'Maio' },
    { value: 6, label: 'Junho' },
    { value: 7, label: 'Julho' },
    { value: 8, label: 'Agosto' },
    { value: 9, label: 'Setembro' },
    { value: 10, label: 'Outubro' },
    { value: 11, label: 'Novembro' },
    { value: 12, label: 'Dezembro' }
  ];
  readonly yearOptions = this.buildYearOptions();

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.router.navigateByUrl('/home', { replaceUrl: true });
      return;
    }

    this.registerForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      birthDay: ['', Validators.required],
      birthMonth: ['', Validators.required],
      birthYear: ['', Validators.required],
      gender: ['']
    }, { validators: [this.passwordMatchValidator, this.birthDateValidator] });
  }

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { mismatch: true };
  }

  birthDateValidator(control: AbstractControl): ValidationErrors | null {
    const day = Number(control.get('birthDay')?.value);
    const month = Number(control.get('birthMonth')?.value);
    const year = Number(control.get('birthYear')?.value);

    if (!day || !month || !year) {
      return null;
    }

    const candidate = new Date(Date.UTC(year, month - 1, day));
    const isValidDate =
      candidate.getUTCFullYear() === year &&
      candidate.getUTCMonth() === month - 1 &&
      candidate.getUTCDate() === day;

    return isValidDate ? null : { invalidBirthDate: true };
  }

  async register() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.registerError = '';

    const { confirmPassword, birthDay, birthMonth, birthYear, ...userData } = this.registerForm.value;
    const dateOfBirth = `${birthYear}-${String(birthMonth).padStart(2, '0')}-${String(birthDay).padStart(2, '0')}`;

    try {
      await firstValueFrom(this.authService.register({
        ...userData,
        dateOfBirth
      }));
      await this.router.navigateByUrl('/home', { replaceUrl: true });
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.registerError = httpError.error?.message || 'Nao foi possivel concluir o cadastro agora.';
      console.error('Registration failed', error);
    }
  }

  goToLogin() {
    this.router.navigateByUrl('/auth/login');
  }

  private buildYearOptions(): number[] {
    const currentYear = new Date().getFullYear();
    const minimumYear = currentYear - 100;

    return Array.from({ length: currentYear - minimumYear + 1 }, (_, index) => currentYear - index);
  }
}
