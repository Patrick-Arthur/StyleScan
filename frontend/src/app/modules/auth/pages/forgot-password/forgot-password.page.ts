import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';

@Component({
  standalone: true,
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.page.html',
  styleUrls: ['./forgot-password.page.scss'],
  imports: [CommonModule, IonicModule, ReactiveFormsModule]
})
export class ForgotPasswordPage implements OnInit {
  forgotPasswordForm: FormGroup = new FormGroup({});
  submitted = false;

  constructor(
    private fb: FormBuilder,
    private router: Router
  ) {}

  ngOnInit() {
    if (localStorage.getItem('authToken')) {
      this.router.navigateByUrl('/home', { replaceUrl: true });
      return;
    }

    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  submit() {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    this.submitted = true;
  }

  goToLogin() {
    this.router.navigateByUrl('/auth/login');
  }

  goToRegister() {
    this.router.navigateByUrl('/auth/register');
  }
}
