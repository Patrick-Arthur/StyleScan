import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class LoginPage implements OnInit {
  loginForm: FormGroup = new FormGroup({});
  loginError = '';

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

    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  async login() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loginError = '';

    try {
      await firstValueFrom(this.authService.login(this.loginForm.value));
      await this.router.navigateByUrl('/home', { replaceUrl: true });
    } catch (error) {
      const httpError = error as HttpErrorResponse;
      this.loginError = httpError.error?.message || 'Nao foi possivel entrar agora. Tente novamente.';
      console.error('Login failed', error);
    }
  }

  goToRegister() {
    this.router.navigateByUrl('/auth/register');
  }

  goToForgotPassword() {
    this.router.navigateByUrl('/auth/forgot-password');
  }
}
