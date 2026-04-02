import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { UserProfile } from 'src/app/modules/user/services/user.service';

export type AccountPlanId = 'free' | 'plus' | 'pro' | 'atelier';
export type LimitedAction = 'avatarSlots' | 'avatarTryOns' | 'realisticRenders' | 'savedLooks';

interface UsageBucket {
  periodKey: string;
  used: number;
}

interface StoredPlanState {
  currentPlanId: AccountPlanId;
  usage: Record<LimitedAction, UsageBucket>;
}

export interface AccountPlanLimitDefinition {
  avatars: number;
  avatarTryOnsPerWeek: number;
  realisticRendersPerMonth: number;
  savedLooks: number;
}

export interface AccountPlanDefinition {
  id: AccountPlanId;
  name: string;
  badge: string;
  price: string;
  summary: string;
  limits: AccountPlanLimitDefinition;
}

export interface LimitCheckResult {
  allowed: boolean;
  reason?: string;
  remaining?: number;
}

export interface AccountPlanSnapshot {
  currentPlan: AccountPlanDefinition;
  usage: Record<LimitedAction, UsageBucket>;
}

@Injectable({
  providedIn: 'root'
})
export class AccountPlanService {
  private readonly storageKey = 'stylescan-account-plan';
  private readonly apiUrl = `${environment.apiUrl}/user`;
  private readonly defaultState: StoredPlanState = {
    currentPlanId: 'free',
    usage: {
      avatarSlots: { periodKey: 'lifetime', used: 0 },
      avatarTryOns: { periodKey: this.getWeekKey(), used: 0 },
      realisticRenders: { periodKey: this.getMonthKey(), used: 0 },
      savedLooks: { periodKey: 'lifetime', used: 0 }
    }
  };

  readonly plans: AccountPlanDefinition[] = [
    {
      id: 'free',
      name: 'Free',
      badge: 'Plano atual',
      price: 'R$ 0',
      summary: 'Entrada para conhecer o app e montar os primeiros looks.',
      limits: {
        avatars: 1,
        avatarTryOnsPerWeek: 6,
        realisticRendersPerMonth: 2,
        savedLooks: 5
      }
    },
    {
      id: 'plus',
      name: 'Style Plus',
      badge: 'Upgrade leve',
      price: 'R$ 1,00',
      summary: 'Mais liberdade para testar combinacoes e manter uma rotina de looks.',
      limits: {
        avatars: 3,
        avatarTryOnsPerWeek: 24,
        realisticRendersPerMonth: 12,
        savedLooks: 30
      }
    },
    {
      id: 'pro',
      name: 'Style Pro',
      badge: 'Mais popular',
      price: 'R$ 39,90',
      summary: 'Plano para uso frequente do provador e uma jornada mais completa.',
      limits: {
        avatars: 8,
        avatarTryOnsPerWeek: 60,
        realisticRendersPerMonth: 30,
        savedLooks: 9999
      }
    },
    {
      id: 'atelier',
      name: 'Style Atelier',
      badge: 'Experiencia premium',
      price: 'R$ 79,90',
      summary: 'Camada premium com folga alta para usuarios intensivos.',
      limits: {
        avatars: 15,
        avatarTryOnsPerWeek: 140,
        realisticRendersPerMonth: 80,
        savedLooks: 9999
      }
    }
  ];

  private readonly state$ = new BehaviorSubject<AccountPlanSnapshot>(this.readState());

  readonly snapshot$ = this.state$.asObservable();

  constructor(private http: HttpClient) {}

  getCurrentPlan(): AccountPlanDefinition {
    return this.state$.value.currentPlan;
  }

  getUsage(): Record<LimitedAction, UsageBucket> {
    return this.state$.value.usage;
  }

  setCurrentPlan(planId: AccountPlanId): void {
    const state = this.hydrateState(this.getStoredState());
    state.currentPlanId = planId;
    this.persistState(state);
  }

  loadRemoteState() {
    return this.http.get<UserProfile>(`${this.apiUrl}/profile`).pipe(
      tap(profile => this.hydrateFromProfile(profile)),
      map(() => this.state$.value)
    );
  }

  updatePlan(planId: AccountPlanId) {
    return this.http.put<UserProfile>(`${this.apiUrl}/plan`, { planId }).pipe(
      tap(profile => this.hydrateFromProfile(profile)),
      map(() => this.state$.value)
    );
  }

  hydrateFromProfile(profile: Pick<UserProfile, 'accountPlan' | 'usage'>): void {
    const state = this.hydrateState(this.getStoredState());
    state.currentPlanId = this.normalizePlanId(profile.accountPlan);
    state.usage = this.mapUsageFromProfile(profile);
    this.persistState(state);
  }

  canCreateAvatar(existingAvatarCount: number): LimitCheckResult {
    const plan = this.getCurrentPlan();
    if (existingAvatarCount >= plan.limits.avatars) {
      return {
        allowed: false,
        reason: `Seu plano ${plan.name} permite ${plan.limits.avatars} avatar${plan.limits.avatars > 1 ? 'es' : ''} ativo${plan.limits.avatars > 1 ? 's' : ''}.`
      };
    }

    return {
      allowed: true,
      remaining: Math.max(plan.limits.avatars - existingAvatarCount, 0)
    };
  }

  canSaveLook(currentSavedLooksCount: number): LimitCheckResult {
    const plan = this.getCurrentPlan();
    if (currentSavedLooksCount >= plan.limits.savedLooks) {
      return {
        allowed: false,
        reason: `Seu plano ${plan.name} permite salvar ate ${this.formatLimit(plan.limits.savedLooks)} looks.`
      };
    }

    return {
      allowed: true,
      remaining: Math.max(plan.limits.savedLooks - currentSavedLooksCount, 0)
    };
  }

  canGenerateAvatarTryOn(): LimitCheckResult {
    return this.canConsume('avatarTryOns', this.getCurrentPlan().limits.avatarTryOnsPerWeek, 'Seu plano atingiu o limite semanal de provas no avatar.');
  }

  canGenerateRealisticRender(): LimitCheckResult {
    return this.canConsume('realisticRenders', this.getCurrentPlan().limits.realisticRendersPerMonth, 'Seu plano atingiu o limite mensal de fotos realistas.');
  }

  registerAvatarTryOn(): void {
    this.consume('avatarTryOns');
  }

  registerRealisticRender(): void {
    this.consume('realisticRenders');
  }

  getUpgradePrompt(action: LimitedAction): string {
    const prompts: Record<LimitedAction, string> = {
      avatarSlots: 'Faca upgrade para liberar mais avatares ativos.',
      avatarTryOns: 'Faca upgrade para testar mais looks no provador toda semana.',
      realisticRenders: 'Faca upgrade para gerar mais fotos realistas com IA.',
      savedLooks: 'Faca upgrade para guardar mais looks no seu acervo.'
    };

    return prompts[action];
  }

  private readState(): AccountPlanSnapshot {
    const stored = this.hydrateState(this.getStoredState());
    return {
      currentPlan: this.resolvePlan(stored.currentPlanId),
      usage: stored.usage
    };
  }

  private getStoredState(): StoredPlanState {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return { ...this.defaultState };
    }

    try {
      return JSON.parse(raw) as StoredPlanState;
    } catch {
      return { ...this.defaultState };
    }
  }

  private hydrateState(state: StoredPlanState): StoredPlanState {
    const weekKey = this.getWeekKey();
    const monthKey = this.getMonthKey();

    return {
      currentPlanId: state.currentPlanId ?? 'free',
      usage: {
        avatarSlots: {
          periodKey: 'lifetime',
          used: state.usage?.avatarSlots?.used ?? 0
        },
        savedLooks: {
          periodKey: 'lifetime',
          used: state.usage?.savedLooks?.used ?? 0
        },
        avatarTryOns: state.usage?.avatarTryOns?.periodKey === weekKey
          ? state.usage.avatarTryOns
          : { periodKey: weekKey, used: 0 },
        realisticRenders: state.usage?.realisticRenders?.periodKey === monthKey
          ? state.usage.realisticRenders
          : { periodKey: monthKey, used: 0 }
      }
    };
  }

  private persistState(state: StoredPlanState): void {
    localStorage.setItem(this.storageKey, JSON.stringify(state));
    this.state$.next({
      currentPlan: this.resolvePlan(state.currentPlanId),
      usage: state.usage
    });
  }

  private canConsume(action: LimitedAction, limit: number, reason: string): LimitCheckResult {
    const state = this.hydrateState(this.getStoredState());
    const used = state.usage[action].used;

    if (used >= limit) {
      return {
        allowed: false,
        reason
      };
    }

    return {
      allowed: true,
      remaining: Math.max(limit - used, 0)
    };
  }

  private consume(action: LimitedAction): void {
    const state = this.hydrateState(this.getStoredState());
    state.usage[action].used += 1;
    this.persistState(state);
  }

  private resolvePlan(planId: AccountPlanId): AccountPlanDefinition {
    return this.plans.find(plan => plan.id === planId) ?? this.plans[0];
  }

  private mapUsageFromProfile(profile: Pick<UserProfile, 'usage'>): Record<LimitedAction, UsageBucket> {
    const weekKey = this.getWeekKey();
    const monthKey = this.getMonthKey();
    const usage = profile.usage ?? [];

    const findUsage = (metricType: string, fallbackPeriodKey: string): UsageBucket => {
      const entry = usage.find(item => item.metricType === metricType && item.periodKey === fallbackPeriodKey)
        ?? usage.find(item => item.metricType === metricType)
        ?? { periodKey: fallbackPeriodKey, used: 0 };

      return {
        periodKey: entry.periodKey,
        used: entry.used
      };
    };

    return {
      avatarSlots: findUsage('avatar_slot', 'lifetime'),
      savedLooks: findUsage('saved_look', 'lifetime'),
      avatarTryOns: findUsage('avatar_try_on', weekKey),
      realisticRenders: findUsage('realistic_render', monthKey)
    };
  }

  private normalizePlanId(planId: string | null | undefined): AccountPlanId {
    const normalized = (planId ?? '').trim().toLowerCase();
    return this.plans.some(plan => plan.id === normalized)
      ? (normalized as AccountPlanId)
      : 'free';
  }

  private formatLimit(limit: number): string {
    return limit >= 9999 ? 'ilimitados' : String(limit);
  }

  private getWeekKey(): string {
    const now = new Date();
    const firstDayOfYear = new Date(now.getFullYear(), 0, 1);
    const dayOffset = Math.floor((now.getTime() - firstDayOfYear.getTime()) / 86400000);
    const week = Math.ceil((dayOffset + firstDayOfYear.getDay() + 1) / 7);
    return `${now.getFullYear()}-W${week}`;
  }

  private getMonthKey(): string {
    const now = new Date();
    return `${now.getFullYear()}-${now.getMonth() + 1}`;
  }
}
