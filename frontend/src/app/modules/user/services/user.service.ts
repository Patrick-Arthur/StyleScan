import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface UserPlanLimits {
  avatars: number;
  avatarTryOnsPerWeek: number;
  realisticRendersPerMonth: number;
  savedLooks: number;
}

export interface UserPlanUsage {
  metricType: string;
  periodKey: string;
  used: number;
}

export interface UserSubscription {
  status: string;
  provider?: string | null;
  reference?: string | null;
  pendingPlanId?: string | null;
  startedAt?: string | null;
  currentPeriodEndsAt?: string | null;
  lastPaymentId?: string | null;
  lastPaymentStatus?: string | null;
  lastPaymentStatusDetail?: string | null;
  lastPaymentUpdatedAt?: string | null;
  lastWebhookReceivedAt?: string | null;
}

export interface GamificationBadge {
  title: string;
  description: string;
  icon: string;
}

export interface GamificationMission {
  title: string;
  description: string;
  progress: number;
  goal: number;
  completed: boolean;
  rewardPoints: number;
}

export interface GamificationSummary {
  currentLevel: number;
  experiencePoints: number;
  nextLevelPoints: number;
  progressPercent: number;
  momentumLabel: string;
  badges: GamificationBadge[];
  missions: GamificationMission[];
}

export interface SubscriptionCheckoutSession {
  checkoutId: string;
  planId: string;
  status: string;
  provider: string;
  checkoutUrl: string;
  sandboxCheckoutUrl?: string | null;
  preferenceId?: string | null;
  expiresAt: string;
  message: string;
  isLiveCheckout: boolean;
}

export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  publicProfileSlug: string;
  dateOfBirth: string;
  gender?: string | null;
  accountPlan: string;
  limits: UserPlanLimits;
  usage: UserPlanUsage[];
  subscription: UserSubscription;
}

export interface UpdateUserProfileRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/user`;

  constructor(private http: HttpClient) {}

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/profile`);
  }

  updateProfile(payload: UpdateUserProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/profile`, payload);
  }

  updateAccountPlan(planId: string): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/plan`, { planId });
  }

  createSubscriptionCheckout(planId: string): Observable<SubscriptionCheckoutSession> {
    return this.http.post<SubscriptionCheckoutSession>(`${this.apiUrl}/subscription/checkout`, { planId });
  }

  getSubscriptionStatus(): Observable<UserSubscription> {
    return this.http.get<UserSubscription>(`${this.apiUrl}/subscription/status`);
  }

  getGamificationSummary(): Observable<GamificationSummary> {
    return this.http.get<GamificationSummary>(`${this.apiUrl}/gamification`);
  }

  registerUsage(metricType: string): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.apiUrl}/usage/register`, { metricType });
  }

  activateSubscription(planId: string, checkoutId: string): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.apiUrl}/subscription/activate`, {
      planId,
      checkoutId,
      provider: 'mercado-pago'
    });
  }
}
