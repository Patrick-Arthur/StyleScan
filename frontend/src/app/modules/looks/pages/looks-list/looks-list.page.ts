import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ActionSheetController, AlertController, IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { AccountPlanService } from 'src/app/core/services/account-plan.service';
import { ShareService } from 'src/app/core/services/share.service';
import { AvatarModel, AvatarService } from '../../../avatar/services/avatar.service';
import { CartService } from '../../../shop/services/cart.service';
import { ProductModel, ShopService } from '../../../shop/services/shop.service';
import { UserService } from '../../../user/services/user.service';
import { LookCollectionModel, LookModel, LooksService, SaveCustomLookPayload, TryOnPreviewHistoryModel, TryOnPreviewModel } from '../../services/looks.service';
import { environment } from 'src/environments/environment';

type StyleBoardId = 'winter' | 'social' | 'casual' | 'smart-casual' | 'emo';
type PieceCategory = 'dress' | 'top' | 'bottom' | 'shoes' | 'accessory';

interface StyleBoard {
  id: StyleBoardId;
  title: string;
  subtitle: string;
  description: string;
  occasion: string;
  accent: string;
  palette: string[];
  categories: PieceCategory[];
  preferredColors: string[];
  keywords: string[];
}

interface AvatarLayer {
  category: PieceCategory;
  style: Record<string, string>;
}

interface SavedLookGroup {
  boardId: StyleBoardId;
  title: string;
  subtitle: string;
  looks: LookModel[];
}

interface BodyStyles {
  head: Record<string, string>;
  neck: Record<string, string>;
  torso: Record<string, string>;
  arms: Record<string, string>;
  legs: Record<string, string>;
}

interface PlanNudge {
  label: string;
  copy: string;
}

interface LookTrait {
  label: string;
  value: string;
}

type SavedLooksSortOption = 'recent' | 'oldest' | 'price-desc' | 'price-asc' | 'with-cover' | 'style';

@Component({
  selector: 'app-looks-list',
  templateUrl: './looks-list.page.html',
  styleUrls: ['./looks-list.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, FormsModule]
})
export class LooksListPage implements OnInit {
  avatars: AvatarModel[] = [];
  allProducts: ProductModel[] = [];
  curatedProducts: ProductModel[] = [];
  savedLooks: LookModel[] = [];
  selectedAvatarId = '';
  selectedBoardId: StyleBoardId = 'casual';
  selectedPieces: Partial<Record<PieceCategory, ProductModel>> = {};
  loading = true;
  savingLook = false;
  generatingTryOn = false;
  generatingRealisticTryOn = false;
  tryOnPreviewUrl = '';
  tryOnUsedAi = false;
  tryOnMode: 'avatar' | 'realistic' = 'avatar';
  stageViewMode: 'result' | 'base' = 'result';
  stageImageErrorCount = 0;
  previewHistory: TryOnPreviewHistoryModel[] = [];
  favoriteLookIds = new Set<string>();
  manualCollections: LookCollectionModel[] = [];
  previewHistoryLoading = false;
  selectedComparisonPreviewId = '';
  customLookName = '';
  customLookNote = '';
  customLookTagsInput = '';
  savedLooksSearchTerm = '';
  savedLooksOccasionFilter = 'all';
  savedLooksTagFilter = 'all';
  savedLooksSort: SavedLooksSortOption = 'recent';
  error = '';
  success = '';
  private lastSavedSelectionKey = '';

  readonly boards: StyleBoard[] = [
    { id: 'winter', title: 'Inverno Urbano', subtitle: 'Camadas limpas e sofisticadas', description: 'Bases neutras, texturas mais fechadas e uma leitura elegante para dias frios.', occasion: 'winter', accent: 'rgba(34, 61, 80, 0.92)', palette: ['#223d50', '#d7c7a5', '#f6f7fb'], categories: ['top', 'bottom', 'shoes', 'accessory'], preferredColors: ['black', 'beige', 'white', 'blue'], keywords: ['classic', 'tailored', 'loafers', 'shirt'] },
    { id: 'social', title: 'Social', subtitle: 'Presenca alinhada para ocasioes formais', description: 'Pecas de alfaiataria, camisa marcante e acabamento mais polido.', occasion: 'formal', accent: 'rgba(24, 39, 47, 0.94)', palette: ['#18272f', '#e8ddcf', '#a69886'], categories: ['top', 'bottom', 'shoes'], preferredColors: ['black', 'beige', 'white'], keywords: ['shirt', 'tailored', 'classic', 'loafers'] },
    { id: 'casual', title: 'Casual', subtitle: 'Conforto com cara de editorial', description: 'Uma base leve para o dia a dia, com tenis, denim e essenciais do guarda-roupa.', occasion: 'casual', accent: 'rgba(48, 102, 110, 0.92)', palette: ['#30666e', '#9ce5f4', '#f4f8f8'], categories: ['top', 'bottom', 'shoes'], preferredColors: ['white', 'blue', 'beige'], keywords: ['essential', 'denim', 'sneakers', 'casual'] },
    { id: 'smart-casual', title: 'Esporte Fino', subtitle: 'Equilibrio entre relax e refinado', description: 'Mistura de estrutura com leveza para jantar, encontro ou trabalho criativo.', occasion: 'smart-casual', accent: 'rgba(93, 73, 53, 0.94)', palette: ['#5d4935', '#efe3d3', '#f8f7f2'], categories: ['top', 'bottom', 'shoes', 'accessory'], preferredColors: ['beige', 'white', 'black', 'gold'], keywords: ['shirt', 'tailored', 'essential', 'statement'] },
    { id: 'emo', title: 'Emo', subtitle: 'Contraste, preto e atitude', description: 'Pecas escuras, detalhes dramaticos e composicoes com presenca mais intensa.', occasion: 'emo', accent: 'rgba(44, 24, 34, 0.95)', palette: ['#2c1822', '#111111', '#aab4c5'], categories: ['top', 'dress', 'shoes', 'accessory'], preferredColors: ['black', 'gold'], keywords: ['black', 'midnight', 'statement', 'dress'] }
  ];

  readonly avatarLayers: AvatarLayer[] = [
    { category: 'dress', style: { top: '25%', left: '31%', width: '38%', height: '34%' } },
    { category: 'top', style: { top: '24%', left: '29%', width: '42%', height: '22%' } },
    { category: 'bottom', style: { top: '45%', left: '32%', width: '36%', height: '22%' } },
    { category: 'shoes', style: { top: '72%', left: '30%', width: '40%', height: '11%' } },
    { category: 'accessory', style: { top: '16%', left: '58%', width: '18%', height: '11%' } }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private accountPlanService: AccountPlanService,
    private avatarService: AvatarService,
    private cartService: CartService,
    private shopService: ShopService,
    private looksService: LooksService,
    private alertController: AlertController,
    private actionSheetController: ActionSheetController,
    private userService: UserService,
    private shareService: ShareService
  ) {}

  ngOnInit(): void {
    void this.loadStudio();
  }

  get currentAvatar(): AvatarModel | null {
    return this.avatars.find(avatar => avatar.id === this.selectedAvatarId) ?? null;
  }

  get activeBoard(): StyleBoard {
    return this.boards.find(board => board.id === this.selectedBoardId) ?? this.boards[0];
  }

  get currentAvatarImageUrl(): string {
    return this.avatarService.resolveAvatarImageUrl(this.currentAvatar);
  }

  get currentAvatarVisualLabel(): string {
    return this.avatarService.resolveAvatarVisualLabel(this.currentAvatar);
  }

  get usingReferenceFallback(): boolean {
    return this.avatarService.resolveAvatarVisualSource(this.currentAvatar) === 'reference';
  }

  get stageImageUrl(): string {
    const avatarGallery = this.avatarService.resolveAvatarGallery(this.currentAvatar);
    const avatarFallback = this.currentAvatarImageUrl || avatarGallery[0];
    const candidates = this.stageViewMode === 'base'
      ? [avatarFallback, ...avatarGallery]
      : [this.tryOnPreviewUrl, avatarFallback, ...avatarGallery];

    const validCandidates = candidates.filter((candidate, index, list) => !!candidate && list.indexOf(candidate) === index);
    return validCandidates[this.stageImageErrorCount] || '';
  }

  get hasGeneratedPreview(): boolean {
    return !!this.tryOnPreviewUrl;
  }

  get currentOrLatestPreviewUrl(): string {
    return this.tryOnPreviewUrl || this.previewHistory[0]?.imageUrl || '';
  }

  get comparisonPreview(): TryOnPreviewHistoryModel | null {
    return this.previewHistory.find(item => item.id === this.selectedComparisonPreviewId) ?? null;
  }

  get comparisonPrimaryLabel(): string {
    if (this.hasGeneratedPreview) {
      return this.tryOnMode === 'realistic' ? 'Resultado atual / Realista' : 'Resultado atual / Avatar';
    }

    return 'Ultimo preview salvo';
  }

  get selectedProducts(): ProductModel[] {
    const order: PieceCategory[] = ['dress', 'top', 'bottom', 'shoes', 'accessory'];
    return order.map(category => this.selectedPieces[category]).filter((item): item is ProductModel => !!item);
  }

  get totalSelectedPrice(): number {
    return this.selectedProducts.reduce((total, product) => total + product.price, 0);
  }

  get currentSelectionSaved(): boolean {
    return !!this.currentSelectionKey && this.currentSelectionKey === this.lastSavedSelectionKey;
  }

  get canShopSelection(): boolean {
    return this.selectedProducts.length > 0;
  }

  get canGenerateTryOn(): boolean {
    return !!this.currentAvatar && this.selectedProducts.length > 0;
  }

  get canSaveLook(): boolean {
    return this.selectedProducts.length >= 2 && !this.currentSelectionSaved && !this.savingLook;
  }

  get selectedCountLabel(): string {
    if (!this.selectedProducts.length) {
      return 'Nenhuma peca no look';
    }

    if (this.selectedProducts.length === 1) {
      return '1 peca no look';
    }

    return `${this.selectedProducts.length} pecas no look`;
  }

  get tryOnHint(): string {
    if (!this.currentAvatar) {
      return 'Escolha um avatar para liberar o provador.';
    }

    if (!this.selectedProducts.length) {
      return 'Adicione pecas ao avatar para testar o look.';
    }

    return 'Voce pode gerar um preview ilustrado ou uma foto realista com as pecas selecionadas.';
  }

  get currentPlanName(): string {
    return this.accountPlanService.getCurrentPlan().name;
  }

  get isPremiumPlan(): boolean {
    return this.accountPlanService.getCurrentPlan().id !== 'free';
  }

  get studioStatusTitle(): string {
    if (this.hasGeneratedPreview && this.stageViewMode === 'result') {
      return this.tryOnMode === 'realistic' ? 'Foto realista gerada' : 'Provador atualizado';
    }

    if (this.stageViewMode === 'base') {
      return 'Base original do avatar';
    }

    return this.isPremiumPlan ? 'Studio premium liberado' : 'Studio essencial';
  }

  get studioStatusCopy(): string {
    if (this.hasGeneratedPreview && this.stageViewMode === 'result') {
      return this.tryOnUsedAi
        ? 'Voce esta vendo o resultado mais recente gerado com IA para esse conjunto.'
        : 'Voce esta vendo a pre-visualizacao mais recente criada para esse conjunto.';
    }

    if (this.stageViewMode === 'base') {
      return 'Use esta visao para comparar a base do avatar com o resultado vestido.';
    }

    return this.isPremiumPlan
      ? 'Seu plano atual libera mais testes e mais margem para explorar o provador.'
      : 'Escolha pecas e gere um preview para ver o look ganhar forma no avatar.';
  }

  get avatarTryOnUsageLabel(): string {
    const usage = this.accountPlanService.getUsage().avatarTryOns.used;
    const limit = this.accountPlanService.getCurrentPlan().limits.avatarTryOnsPerWeek;
    return `${usage}/${limit} provas no avatar nesta semana`;
  }

  get realisticUsageLabel(): string {
    const usage = this.accountPlanService.getUsage().realisticRenders.used;
    const limit = this.accountPlanService.getCurrentPlan().limits.realisticRendersPerMonth;
    return `${usage}/${limit} fotos realistas neste mes`;
  }

  get planNudges(): PlanNudge[] {
    const plan = this.accountPlanService.getCurrentPlan();
    const usage = this.accountPlanService.getUsage();
    const nudges: PlanNudge[] = [];
    const tryOnRemaining = Math.max(plan.limits.avatarTryOnsPerWeek - usage.avatarTryOns.used, 0);
    const realisticRemaining = Math.max(plan.limits.realisticRendersPerMonth - usage.realisticRenders.used, 0);
    const savedLooksRemaining = plan.limits.savedLooks >= 9999
      ? 9999
      : Math.max(plan.limits.savedLooks - this.savedLooks.length, 0);

    if (tryOnRemaining <= 3) {
      nudges.push({
        label: 'Provas no avatar',
        copy: tryOnRemaining <= 0
          ? 'Seu limite semanal de provas foi atingido. Um upgrade libera novas tentativas no studio.'
          : `Restam ${tryOnRemaining} provas no avatar nesta semana. Vale abrir os planos antes de travar o studio.`
      });
    }

    if (realisticRemaining <= 2) {
      nudges.push({
        label: 'Fotos realistas',
        copy: realisticRemaining <= 0
          ? 'As fotos realistas deste mes acabaram no seu plano atual. Um upgrade destrava novas geracoes.'
          : `Restam ${realisticRemaining} fotos realistas neste mes. O plano premium da mais folga para IA.`
      });
    }

    if (savedLooksRemaining <= 2 && plan.limits.savedLooks < 9999) {
      nudges.push({
        label: 'Looks salvos',
        copy: savedLooksRemaining <= 0
          ? 'Seu acervo chegou ao limite do plano atual. Um upgrade abre mais espaco para colecoes.'
          : `Seu acervo esta perto do limite, com ${savedLooksRemaining} vaga${savedLooksRemaining > 1 ? 's' : ''} restante${savedLooksRemaining > 1 ? 's' : ''}.`
      });
    }

    return nudges;
  }

  get avatarCanvasStyle(): Record<string, string> {
    const palette = this.activeBoard.palette;
    return {
      background: `radial-gradient(circle at top, ${palette[1]}66, transparent 52%), linear-gradient(180deg, ${palette[2]} 0%, #ffffff 100%)`
    };
  }

  get mannequinStyle(): Record<string, string> {
    const avatar = this.currentAvatar;
    if (!avatar) {
      return {};
    }

    const width = this.clamp(190 + ((avatar.hips - 90) * 1.1), 188, 232);
    const height = this.clamp(296 + ((avatar.height - 165) * 0.7), 296, 340);
    return {
      width: `${width}px`,
      height: `${height}px`
    };
  }

  get bodyStyles(): BodyStyles {
    const avatar = this.currentAvatar;
    const skin = this.resolveSkinGradient(avatar?.skinTone);

    if (!avatar) {
      return {
        head: { background: skin },
        neck: { background: skin },
        torso: { background: skin },
        arms: { background: skin },
        legs: { background: skin }
      };
    }

    const torsoWidth = this.clamp(94 + ((avatar.chest - avatar.waist) * 0.45) + ((avatar.bodyType.toLowerCase().includes('curv') || avatar.bodyType.toLowerCase().includes('ampulheta')) ? 8 : 0), 92, 126);
    const torsoHeight = this.clamp(126 + ((avatar.height - 165) * 0.35), 124, 150);
    const armsWidth = this.clamp(torsoWidth + 48, 142, 176);
    const legsWidth = this.clamp(86 + ((avatar.hips - avatar.waist) * 0.55), 82, 114);

    return {
      head: { background: skin },
      neck: { background: skin },
      torso: { background: skin, width: `${torsoWidth}px`, height: `${torsoHeight}px` },
      arms: { background: skin, width: `${armsWidth}px` },
      legs: { background: skin, width: `${legsWidth}px` }
    };
  }

  get fitHighlights(): string[] {
    const avatar = this.currentAvatar;
    if (!avatar) {
      return [];
    }

    return [
      `${avatar.height} cm`,
      `Busto ${avatar.chest} cm`,
      `Cintura ${avatar.waist} cm`,
      `Quadril ${avatar.hips} cm`
    ];
  }

  get fitNote(): string {
    const avatar = this.currentAvatar;
    if (!avatar) {
      return 'Selecione um avatar para ajustar a montagem.';
    }

    if (avatar.bodyType.toLowerCase().includes('tri')) {
      return 'A composicao privilegia equilibrio visual entre tronco e base.';
    }

    if (avatar.bodyType.toLowerCase().includes('ret')) {
      return 'As pecas escolhidas ajudam a criar uma leitura mais estruturada.';
    }

    if (avatar.bodyType.toLowerCase().includes('ampulheta') || avatar.bodyType.toLowerCase().includes('curv')) {
      return 'A montagem destaca a silhueta e mantem proporcoes mais marcadas.';
    }

    return 'O provador usa suas medidas para sugerir uma silhueta mais fiel no app.';
  }

  get lookTraits(): LookTrait[] {
    if (!this.selectedProducts.length) {
      return [];
    }

    const dominantColors = Array.from(new Set(this.selectedProducts.map(product => this.toTitleCase(product.color)))).slice(0, 2);
    const categories = this.selectedProducts.map(product => this.toTitleCase(product.category)).join(' + ');
    const ratingAverage = this.selectedProducts.reduce((total, product) => total + product.rating, 0) / this.selectedProducts.length;

    return [
      { label: 'Paleta', value: dominantColors.join(' / ') || 'Neutra' },
      { label: 'Composicao', value: categories },
      { label: 'Curadoria', value: `${ratingAverage.toFixed(1)} de media` }
    ];
  }

  get featuredSavedLook(): LookModel | null {
    const featured = this.filteredSavedLooks.find(look => this.resolveBoardId(look) === this.selectedBoardId);
    return featured ?? this.filteredSavedLooks[0] ?? null;
  }

  get currentSavedLook(): LookModel | null {
    if (!this.currentSelectionKey) {
      return null;
    }

    return this.savedLooks.find(look => this.buildSelectionKey(look.avatarId, this.resolveBoardId(look), look.items.map(item => item.id)) === this.currentSelectionKey) ?? null;
  }

  get availableSavedLookOccasions(): string[] {
    return Array.from(new Set(this.savedLooks.map(look => look.occasion).filter(Boolean))).sort((left, right) => left.localeCompare(right));
  }

  get availableSavedLookTags(): string[] {
    const tags = this.savedLooks.reduce((allTags: string[], look) => {
      for (const tag of look.occasionTags ?? []) {
        if (tag) {
          allTags.push(tag);
        }
      }

      return allTags;
    }, []);

    return Array.from(new Set(tags)).sort((left, right) => left.localeCompare(right));
  }

  get filteredSavedLooks(): LookModel[] {
    const search = this.savedLooksSearchTerm.trim().toLowerCase();

    return this.savedLooks
      .filter(look => {
        const matchesSearch = !search || [
          look.name,
          look.style,
          look.occasion,
          look.note ?? '',
          ...(look.occasionTags ?? [])
        ].some(value => value.toLowerCase().includes(search));

        const matchesOccasion = this.savedLooksOccasionFilter === 'all'
          || look.occasion.toLowerCase() === this.savedLooksOccasionFilter.toLowerCase();

        const matchesTag = this.savedLooksTagFilter === 'all'
          || (look.occasionTags ?? []).some(tag => tag.toLowerCase() === this.savedLooksTagFilter.toLowerCase());

        return matchesSearch && matchesOccasion && matchesTag;
      })
      .sort((left, right) => this.sortSavedLooks(left, right));
  }

  get activeCoverPreviewUrl(): string {
    return this.comparisonPreview?.imageUrl || this.tryOnPreviewUrl;
  }

  get canPromotePreviewToCover(): boolean {
    return !!this.currentSavedLook && !!this.activeCoverPreviewUrl;
  }

  get currentSavedLookCollections(): LookCollectionModel[] {
    if (!this.currentSavedLook) {
      return [];
    }

    return this.manualCollections.filter(collection => collection.looks.some(look => look.id === this.currentSavedLook?.id));
  }

  get suggestedLookName(): string {
    if (!this.currentAvatar) {
      return this.activeBoard.title;
    }

    return `${this.currentAvatar.name} - ${this.activeBoard.title}`;
  }

  get parsedOccasionTags(): string[] {
    return this.customLookTagsInput
      .split(',')
      .map(tag => tag.trim())
      .filter(Boolean)
      .filter((tag, index, tags) => tags.findIndex(existingTag => existingTag.toLowerCase() === tag.toLowerCase()) === index)
      .slice(0, 6);
  }

  get groupedSavedLooks(): SavedLookGroup[] {
    return this.boards
      .map(board => ({
        boardId: board.id,
        title: board.title,
        subtitle: board.subtitle,
        looks: this.filteredSavedLooks.filter(look => this.resolveBoardId(look) === board.id)
      }))
      .filter(group => group.looks.length > 0);
  }

  get hasSavedLookFiltersApplied(): boolean {
    return !!this.savedLooksSearchTerm.trim()
      || this.savedLooksOccasionFilter !== 'all'
      || this.savedLooksTagFilter !== 'all'
      || this.savedLooksSort !== 'recent';
  }

  private get currentSelectionKey(): string {
    if (!this.currentAvatar || this.selectedProducts.length < 2) {
      return '';
    }

    return this.buildSelectionKey(this.currentAvatar.id, this.selectedBoardId, this.selectedProducts.map(product => product.id));
  }

  async loadStudio(): Promise<void> {
    this.loading = true;
    this.error = '';

    try {
      const [avatarResponse, productsResponse, looksResponse, favoriteResponse, collectionsResponse] = await Promise.all([
        firstValueFrom(this.avatarService.getUserAvatars()),
        firstValueFrom(this.shopService.getProducts(undefined, undefined, undefined, 1, 100)),
        firstValueFrom(this.looksService.getUserLooks()),
        firstValueFrom(this.looksService.getFavoriteLooks()),
        firstValueFrom(this.looksService.getLookCollections()),
        firstValueFrom(this.accountPlanService.loadRemoteState())
      ]);

      this.avatars = avatarResponse.data;
      this.allProducts = productsResponse.data;
      this.savedLooks = looksResponse.data;
      this.favoriteLookIds = new Set(favoriteResponse.data.map(look => look.id));
      this.manualCollections = collectionsResponse.data;

        if (this.avatars.length > 0 && !this.selectedAvatarId) {
          this.selectedAvatarId = this.avatars[0].id;
        }

        await this.hydrateSelectedAvatar();

        if (!this.customLookName) {
          this.customLookName = this.suggestedLookName;
        }

      this.restoreLookFromRoute();
      this.applyBoard(this.selectedBoardId);
      await this.loadPreviewHistory();
    } catch (error) {
      this.handleError(error, 'Nao foi possivel carregar o studio de looks.');
    } finally {
      this.loading = false;
    }
  }

  goHome(): void {
    this.router.navigateByUrl('/home');
  }

  goToCreateAvatar(): void {
    this.router.navigateByUrl('/avatar/create');
  }

  goToUpgrade(): void {
    this.router.navigateByUrl('/user/upgrade');
  }

  async onAvatarChange(): Promise<void> {
    await this.hydrateSelectedAvatar();
    this.resetPreviewState();
    this.stageViewMode = 'result';
    this.selectedComparisonPreviewId = '';
    if (!this.currentSavedLook) {
      this.customLookName = this.suggestedLookName;
    }
    this.success = '';
    await this.loadPreviewHistory();
  }

  applyBoard(boardId: StyleBoardId, preservePreview = false): void {
    this.selectedBoardId = boardId;
    if (!preservePreview) {
      this.resetPreviewState();
    }
    const board = this.activeBoard;

    this.curatedProducts = [...this.allProducts]
      .map(product => ({ product, score: this.scoreProduct(product, board) }))
      .filter(entry => entry.score > 0)
      .sort((left, right) => right.score - left.score || left.product.price - right.product.price)
      .map(entry => entry.product)
      .slice(0, 24);

    if (!this.curatedProducts.length) {
      this.curatedProducts = this.allProducts.slice(0, 24);
    }

    this.syncSavedSelectionState();
    if (!this.currentSavedLook && (!this.customLookName || this.customLookName === this.suggestedLookName)) {
      this.customLookName = this.suggestedLookName;
    }
    this.success = '';
  }

  addPiece(product: ProductModel): void {
    const category = product.category.toLowerCase() as PieceCategory;
    const nextSelection = { ...this.selectedPieces };

    if (category === 'dress') {
      delete nextSelection.top;
      delete nextSelection.bottom;
    }

    if (category === 'top' || category === 'bottom') {
      delete nextSelection.dress;
    }

    nextSelection[category] = product;
    this.selectedPieces = nextSelection;
    this.resetPreviewState();
    this.stageViewMode = 'result';
    this.syncSavedSelectionState();
    this.success = `${product.name} foi adicionado ao avatar.`;
    this.error = '';
  }

  removePiece(category: PieceCategory): void {
    delete this.selectedPieces[category];
    this.selectedPieces = { ...this.selectedPieces };
    this.resetPreviewState();
    this.stageViewMode = 'result';
    this.syncSavedSelectionState();
    this.success = 'Peca removida do avatar.';
  }

  removeProduct(product: ProductModel): void {
    this.removePiece(product.category.toLowerCase() as PieceCategory);
  }

  isSelected(productId: string): boolean {
    return this.selectedProducts.some(product => product.id === productId);
  }

  openProduct(productId: string): void {
    this.router.navigateByUrl(`/shop/product/${productId}`);
  }

  openStore(product: ProductModel): void {
    const targetUrl = product.productUrl || product.storeUrl;
    if (!targetUrl) {
      this.error = 'Nao encontramos o link externo dessa peca.';
      return;
    }

    window.open(targetUrl, '_blank', 'noopener');
  }

  addToCart(product: ProductModel): void {
    this.cartService.addProduct(product);
    this.success = `${product.name} foi adicionado ao carrinho.`;
    this.error = '';
  }

  shopSelectedLook(): void {
    if (!this.selectedProducts.length) {
      this.error = 'Selecione ao menos uma peca para abrir as lojas.';
      return;
    }

    this.selectedProducts.forEach(product => this.openStore(product));
    void this.registerPurchaseIntent();
    this.success = 'Abrimos as paginas externas das pecas selecionadas.';
  }

  clearSelection(): void {
    this.selectedPieces = {};
    this.lastSavedSelectionKey = '';
    this.resetPreviewState();
    this.stageViewMode = 'result';
    this.selectedComparisonPreviewId = '';
    this.curatedProducts = [...this.curatedProducts];
    this.customLookName = this.suggestedLookName;
    this.customLookNote = '';
    this.customLookTagsInput = '';
    this.success = 'Avatar limpo para uma nova composicao.';
    this.error = '';
  }

  onStageImageError(): void {
    this.stageImageErrorCount += 1;
  }

  clearSavedLookFilters(): void {
    this.savedLooksSearchTerm = '';
    this.savedLooksOccasionFilter = 'all';
    this.savedLooksTagFilter = 'all';
    this.savedLooksSort = 'recent';
  }

  async generateTryOnPreview(mode: 'avatar' | 'realistic' = 'avatar'): Promise<void> {
    if (!this.currentAvatar || !this.selectedProducts.length) {
      return;
    }

    if (mode === 'realistic' && this.generatingRealisticTryOn) {
      return;
    }

    if (mode === 'avatar' && this.generatingTryOn) {
      return;
    }

    const limitCheck = mode === 'realistic'
      ? this.accountPlanService.canGenerateRealisticRender()
      : this.accountPlanService.canGenerateAvatarTryOn();

    if (!limitCheck.allowed) {
      this.error = `${limitCheck.reason} ${this.accountPlanService.getUpgradePrompt(mode === 'realistic' ? 'realisticRenders' : 'avatarTryOns')}`;
      return;
    }

    this.tryOnMode = mode;
    if (mode === 'realistic') {
      this.generatingRealisticTryOn = true;
    } else {
      this.generatingTryOn = true;
    }
    this.error = '';

    try {
      const preview: TryOnPreviewModel = await firstValueFrom(this.looksService.generateTryOnPreview({
        avatarId: this.currentAvatar.id,
        style: this.activeBoard.title,
        occasion: this.activeBoard.occasion,
        boardId: this.activeBoard.id,
        palette: this.activeBoard.palette,
        mode,
        productIds: this.selectedProducts.map(product => product.id)
      }));

      this.tryOnPreviewUrl = this.appendVersion(this.avatarService.resolveAbsoluteUrl(preview.imageUrl));
      this.tryOnUsedAi = preview.usedAi;
      this.tryOnMode = mode;
      this.stageViewMode = 'result';
      this.stageImageErrorCount = 0;
      await firstValueFrom(this.accountPlanService.loadRemoteState());
      await this.loadPreviewHistory();
      this.success = preview.usedAi
        ? (mode === 'realistic'
            ? 'Foto realista atualizada com as pecas selecionadas.'
            : 'Provador IA atualizado com as pecas selecionadas.')
        : 'Preview rapido do provador atualizado.';
    } catch (error) {
      this.handleError(error, 'Nao foi possivel gerar o preview do provador agora.');
    } finally {
      if (mode === 'realistic') {
        this.generatingRealisticTryOn = false;
      } else {
        this.generatingTryOn = false;
      }
    }
  }
  async saveCurrentLook(): Promise<void> {
    const payload = this.buildSavePayload();
    if (!payload || this.savingLook) {
      return;
    }

    const saveLimitCheck = this.accountPlanService.canSaveLook(this.savedLooks.length);
    if (!saveLimitCheck.allowed) {
      this.error = `${saveLimitCheck.reason} ${this.accountPlanService.getUpgradePrompt('savedLooks')}`;
      return;
    }

    if (this.currentSelectionSaved) {
      this.success = 'Esse look ja foi salvo para este avatar.';
      return;
    }

    this.savingLook = true;
    this.error = '';

    try {
      const savedLook = await firstValueFrom(this.looksService.saveCustomLook(payload));
      this.lastSavedSelectionKey = this.currentSelectionKey;
      this.savedLooks = [savedLook, ...this.savedLooks.filter(look => look.id !== savedLook.id)];
      await this.reloadCollections();
      await firstValueFrom(this.accountPlanService.loadRemoteState());
      this.success = 'Look salvo com sucesso no seu perfil.';
    } catch (error) {
      this.handleError(error, 'Nao foi possivel salvar esse look agora.');
    } finally {
      this.savingLook = false;
    }
  }

  async promotePreviewToCover(): Promise<void> {
    if (!this.currentSavedLook || !this.activeCoverPreviewUrl) {
      return;
    }

    try {
      const updatedLook = await firstValueFrom(this.looksService.updateLookCover(this.currentSavedLook.id, {
        heroImageUrl: this.normalizeAssetUrl(this.activeCoverPreviewUrl),
        heroPreviewMode: this.comparisonPreview?.mode || this.tryOnMode
      }));

      this.savedLooks = [updatedLook, ...this.savedLooks.filter(look => look.id !== updatedLook.id)];
      this.success = 'Esse preview agora e a capa oficial do look salvo.';
      this.error = '';
    } catch (error) {
      this.handleError(error, 'Nao foi possivel atualizar a capa do look agora.');
    }
  }

  async addCurrentLookToCollection(): Promise<void> {
    if (!this.currentSavedLook) {
      return;
    }

    const alert = await this.alertController.create({
      header: 'Adicionar a colecao',
      inputs: [
        {
          name: 'name',
          type: 'text',
          placeholder: 'Ex.: Trabalho, Viagem, Noite'
        }
      ],
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Salvar',
          handler: async value => {
            const name = (value?.name ?? '').trim();
            if (!name) {
              return false;
            }

            try {
              await firstValueFrom(this.looksService.addLookToCollection(this.currentSavedLook!.id, name));
              await this.reloadCollections();
              this.success = 'Look salvo organizado em colecao.';
              this.error = '';
              return true;
            } catch (error) {
              this.handleError(error, 'Nao foi possivel adicionar o look a colecao agora.');
              return false;
            }
          }
        }
      ]
    });

    await alert.present();
  }

  async removeCurrentLookFromCollection(collection: LookCollectionModel): Promise<void> {
    if (!this.currentSavedLook) {
      return;
    }

    try {
      await firstValueFrom(this.looksService.removeLookFromCollection(this.currentSavedLook.id, collection.id));
      await this.reloadCollections();
      this.success = 'Look removido da colecao.';
      this.error = '';
    } catch (error) {
      this.handleError(error, 'Nao foi possivel remover o look da colecao agora.');
    }
  }

  openLookDetail(lookId: string): void {
    this.router.navigate(['/looks', lookId]);
  }

  isFavoriteLook(lookId: string): boolean {
    return this.favoriteLookIds.has(lookId);
  }

  async toggleFavoriteLook(look: LookModel, event?: Event): Promise<void> {
    event?.stopPropagation();

    try {
      if (this.isFavoriteLook(look.id)) {
        await firstValueFrom(this.looksService.removeLookFromFavorites(look.id));
        this.favoriteLookIds.delete(look.id);
        this.favoriteLookIds = new Set(this.favoriteLookIds);
        this.success = 'Look removido dos favoritos.';
      } else {
        await firstValueFrom(this.looksService.addLookToFavorites(look.id));
        this.favoriteLookIds.add(look.id);
        this.favoriteLookIds = new Set(this.favoriteLookIds);
        this.success = 'Look adicionado aos favoritos.';
      }

      this.error = '';
    } catch (error) {
      this.handleError(error, 'Nao foi possivel atualizar os favoritos agora.');
    }
  }

  shopSavedLook(look: LookModel): void {
    const urls = Array.from(new Set(look.items.map(item => item.productUrl).filter(Boolean)));
    if (!urls.length) {
      this.error = 'Nao encontramos links externos para esse look.';
      return;
    }

    urls.forEach(url => window.open(url, '_blank', 'noopener'));
    void this.registerPurchaseIntent();
    this.success = 'Abrimos as lojas externas para esse look.';
  }

  async shareCurrentResult(): Promise<void> {
    const imageUrl = this.activeCoverPreviewUrl
      || this.currentOrLatestPreviewUrl
      || this.tryOnPreviewUrl
      || this.currentSavedLook?.heroImageUrl
      || this.currentAvatarImageUrl
      || '';
    const title = this.currentSavedLook?.name || this.customLookName || this.activeBoard.title;
    const text = `Montei um look no StyleScan: ${title} • ${this.activeBoard.title} • ${this.selectedProducts.length} pecas.`;
    const url = this.currentSavedLook?.isPublished && this.currentSavedLook.shareSlug
      ? `${environment.publicSiteUrl}/look/${this.currentSavedLook.shareSlug}`
      : this.currentSavedLook
        ? `${window.location.origin}/looks/${this.currentSavedLook.id}`
        : environment.publicSiteUrl;

    const payload = {
      title: `StyleScan | ${title}`,
      text,
      url,
      imageUrl
    };

    const actionSheet = await this.actionSheetController.create({
      header: 'Compartilhar resultado',
      subHeader: 'Escolha onde deseja divulgar esse preview.',
      buttons: [
        {
          text: 'WhatsApp',
          handler: () => void this.handleShareAction(() => this.shareService.openWhatsApp(payload), 'Link pronto para enviar no WhatsApp.')
        },
        {
          text: 'Facebook',
          handler: () => void this.handleShareAction(() => this.shareService.openFacebook(payload), 'Link aberto para compartilhar no Facebook.')
        },
        {
          text: 'Instagram',
          handler: () => void this.handleShareAction(() => this.shareService.prepareInstagram(payload), 'Imagem baixada e legenda copiada para publicar no Instagram.')
        },
        {
          text: 'Baixar imagem',
          handler: () => void this.handleShareAction(() => this.shareService.downloadImage(payload.imageUrl, title), 'Imagem baixada com sucesso.')
        },
        {
          text: 'Copiar legenda',
          handler: () => void this.handleShareAction(() => this.shareService.copyCaption(payload), 'Legenda copiada.')
        },
        {
          text: 'Mais opcoes do dispositivo',
          handler: () => void this.handleShareAction(() => this.shareService.shareNative(payload), 'Resultado pronto para compartilhar.')
        },
        {
          text: 'Cancelar',
          role: 'cancel'
        }
      ]
    });

    await actionSheet.present();
  }

  reloadSavedLook(look: LookModel): void {
    this.selectedAvatarId = look.avatarId;
    this.selectedBoardId = this.resolveBoardId(look);
    this.selectedPieces = {};
    this.resetPreviewState();
    this.stageViewMode = 'result';

    for (const item of look.items) {
      const product = this.allProducts.find(existingProduct => existingProduct.id === item.id);
      if (product) {
        this.selectedPieces[item.category.toLowerCase() as PieceCategory] = product;
      }
    }

    this.lastSavedSelectionKey = this.buildSelectionKey(look.avatarId, this.selectedBoardId, look.items.map(item => item.id));
    this.applyBoard(this.selectedBoardId);
    void this.hydrateSelectedAvatar();
    void this.loadPreviewHistory();
    this.customLookName = look.name;
    this.customLookNote = look.note ?? '';
    this.customLookTagsInput = (look.occasionTags ?? []).join(', ');
    this.success = 'Look salvo carregado novamente no studio.';
  }

  applyPreviewHistoryItem(item: TryOnPreviewHistoryModel): void {
    if (this.selectedAvatarId !== item.avatarId) {
      this.selectedAvatarId = item.avatarId;
      void this.hydrateSelectedAvatar();
    }

    if (item.boardId && this.boards.some(board => board.id === item.boardId)) {
      this.applyBoard(item.boardId as StyleBoardId, true);
    }

    this.tryOnPreviewUrl = this.appendVersion(this.avatarService.resolveAbsoluteUrl(item.imageUrl));
    this.tryOnUsedAi = item.usedAi;
    this.tryOnMode = item.mode;
    this.stageViewMode = 'result';
    this.stageImageErrorCount = 0;
    this.success = 'Preview anterior reaplicado no studio.';
    this.error = '';
  }

  togglePreviewComparison(item: TryOnPreviewHistoryModel): void {
    this.selectedComparisonPreviewId = this.selectedComparisonPreviewId === item.id ? '' : item.id;
  }

  clearPreviewComparison(): void {
    this.selectedComparisonPreviewId = '';
  }

  getLayerProduct(category: PieceCategory): ProductModel | undefined {
    return this.selectedPieces[category];
  }

  getLayerBackdropStyle(category: PieceCategory): Record<string, string> {
    const product = this.getLayerProduct(category);
    const color = this.resolveColorHex(product?.color);

    const radiusMap: Record<PieceCategory, string> = {
      dress: '34px 34px 26px 26px',
      top: '30px 30px 18px 18px',
      bottom: '20px 20px 24px 24px',
      shoes: '22px',
      accessory: '18px'
    };

    return {
      '--layer-tint': color,
      borderRadius: radiusMap[category]
    };
  }

  getLayerLabel(category: PieceCategory): string {
    const labels: Record<PieceCategory, string> = {
      dress: 'Vestido',
      top: 'Parte superior',
      bottom: 'Parte inferior',
      shoes: 'Calcados',
      accessory: 'Acessorio'
    };

    return labels[category];
  }

  getBoardImageUrl(boardId: StyleBoardId): string {
    const imageMap: Record<StyleBoardId, string> = {
      winter: 'https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?auto=format&fit=crop&w=900&q=80',
      social: 'https://images.unsplash.com/photo-1594938298603-c8148c4dae35?auto=format&fit=crop&w=900&q=80',
      casual: 'https://images.unsplash.com/photo-1483985988355-763728e1935b?auto=format&fit=crop&w=900&q=80',
      'smart-casual': 'https://images.unsplash.com/photo-1529139574466-a303027c1d8b?auto=format&fit=crop&w=900&q=80',
      emo: 'https://images.unsplash.com/photo-1517841905240-472988babdf9?auto=format&fit=crop&w=900&q=80'
    };

    return imageMap[boardId];
  }

  trackByBoard(_: number, board: StyleBoard): string {
    return board.id;
  }

  trackByProduct(_: number, product: ProductModel): string {
    return product.id;
  }

  trackByLook(_: number, look: LookModel): string {
    return look.id;
  }

  trackByLookGroup(_: number, group: SavedLookGroup): string {
    return group.boardId;
  }

  trackByPreviewHistory(_: number, item: TryOnPreviewHistoryModel): string {
    return item.id;
  }

  private async loadPreviewHistory(): Promise<void> {
    if (!this.selectedAvatarId) {
      this.previewHistory = [];
      return;
    }

    this.previewHistoryLoading = true;
    try {
      const response = await firstValueFrom(this.looksService.getPreviewHistory(this.selectedAvatarId, 10));
      this.previewHistory = response.data.map(item => ({
        ...item,
        imageUrl: this.appendVersion(this.avatarService.resolveAbsoluteUrl(item.imageUrl))
      }));

      if (this.selectedComparisonPreviewId && !this.previewHistory.some(item => item.id === this.selectedComparisonPreviewId)) {
        this.selectedComparisonPreviewId = '';
      }
    } catch (error) {
      this.previewHistory = [];
      this.selectedComparisonPreviewId = '';
      console.error(error);
    } finally {
      this.previewHistoryLoading = false;
    }
  }

  private async reloadCollections(): Promise<void> {
    const collectionsResponse = await firstValueFrom(this.looksService.getLookCollections());
    this.manualCollections = collectionsResponse.data;
  }

  private async registerSharedLook(): Promise<void> {
    try {
      await firstValueFrom(this.userService.registerUsage('shared_look'));
    } catch (error) {
      console.error(error);
    }
  }

  private async registerPurchaseIntent(): Promise<void> {
    try {
      await firstValueFrom(this.userService.registerUsage('purchase_click'));
    } catch (error) {
      console.error(error);
    }
  }

  private buildSavePayload(): SaveCustomLookPayload | null {
    if (!this.currentAvatar || this.selectedProducts.length < 2) {
      this.error = 'Escolha um avatar e selecione pelo menos duas pecas para salvar o look.';
      return null;
    }

    return {
      avatarId: this.currentAvatar.id,
      name: this.customLookName?.trim() || this.suggestedLookName,
      occasion: this.activeBoard.occasion,
      style: this.activeBoard.title,
      season: this.activeBoard.title,
      note: this.customLookNote?.trim() || undefined,
      occasionTags: this.parsedOccasionTags,
      heroImageUrl: this.tryOnPreviewUrl ? this.normalizeAssetUrl(this.tryOnPreviewUrl) : undefined,
      heroPreviewMode: this.tryOnPreviewUrl ? this.tryOnMode : undefined,
      productIds: this.selectedProducts.map(product => product.id)
    };
  }

  private async hydrateSelectedAvatar(): Promise<void> {
    if (!this.selectedAvatarId) {
      return;
    }

    try {
      const avatar = await firstValueFrom(this.avatarService.getAvatarById(this.selectedAvatarId));
      const avatarIndex = this.avatars.findIndex(existingAvatar => existingAvatar.id === avatar.id);

      if (avatarIndex >= 0) {
        this.avatars = this.avatars.map((existingAvatar, index) => index === avatarIndex ? avatar : existingAvatar);
        return;
      }

      this.avatars = [...this.avatars, avatar];
    } catch (error) {
      console.error('Nao foi possivel hidratar o avatar selecionado no studio.', error);
    }
  }

  private restoreLookFromRoute(): void {
    const lookId = this.route.snapshot.queryParamMap.get('lookId');
    if (!lookId) {
      return;
    }

    const look = this.savedLooks.find(existingLook => existingLook.id === lookId);
    if (look) {
      this.reloadSavedLook(look);
    }
  }

  private resolveBoardId(look: LookModel): StyleBoardId {
    const normalized = `${look.style} ${look.occasion}`.toLowerCase();

    if (normalized.includes('inverno') || normalized.includes('winter')) return 'winter';
    if (normalized.includes('social') || normalized.includes('formal')) return 'social';
    if (normalized.includes('esporte fino') || normalized.includes('smart')) return 'smart-casual';
    if (normalized.includes('emo')) return 'emo';
    return 'casual';
  }

  private buildSelectionKey(avatarId: string, boardId: StyleBoardId, productIds: string[]): string {
    return [avatarId, boardId, ...productIds].join('|');
  }

  private syncSavedSelectionState(): void {
    if (!this.currentSelectionKey || this.currentSelectionKey !== this.lastSavedSelectionKey) {
      this.lastSavedSelectionKey = '';
    }
  }

  private scoreProduct(product: ProductModel, board: StyleBoard): number {
    const category = product.category.toLowerCase() as PieceCategory;
    const color = product.color.toLowerCase();
    const name = product.name.toLowerCase();
    const description = product.description.toLowerCase();
    let score = 0;

    if (board.categories.includes(category)) score += 4;
    if (board.preferredColors.includes(color)) score += 3;

    for (const keyword of board.keywords) {
      if (name.includes(keyword) || description.includes(keyword)) score += 2;
    }

    if (product.rating >= 4.7) score += 1;
    return score;
  }

  private resolveSkinGradient(skinTone?: string): string {
    const tone = (skinTone ?? '').toLowerCase();

    if (tone.includes('esc')) return 'linear-gradient(180deg,#8a5b3f,#6a432d)';
    if (tone.includes('neg')) return 'linear-gradient(180deg,#6f4632,#4d3023)';
    if (tone.includes('mor')) return 'linear-gradient(180deg,#c08a65,#9a6847)';
    return 'linear-gradient(180deg,#f3d7bd,#d8b08d)';
  }

  private resolveColorHex(color?: string): string {
    const normalized = (color ?? '').trim().toLowerCase();
    const colorMap: Record<string, string> = {
      black: '#1a1b20',
      white: '#f6f7f8',
      blue: '#4976a9',
      beige: '#d8c1a1',
      gold: '#caa46a',
      red: '#b84d4d',
      pink: '#d48a9d',
      brown: '#7a5d47',
      grey: '#8b97a1',
      gray: '#8b97a1',
      green: '#537d63'
    };

    return colorMap[normalized] ?? '#87b8c3';
  }

  private toTitleCase(value: string): string {
    return value
      .split(/[\s-]+/)
      .filter(Boolean)
      .map(part => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ');
  }

  private clamp(value: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, value));
  }

  private appendVersion(url: string): string {
    if (!url) {
      return '';
    }

    const separator = url.includes('?') ? '&' : '?';
    return `${url}${separator}v=${Date.now()}`;
  }

  private normalizeAssetUrl(url: string): string {
    if (!url) {
      return '';
    }

    try {
      if (url.startsWith('http://') || url.startsWith('https://')) {
        const parsed = new URL(url);
        return `${parsed.origin}${parsed.pathname}`;
      }

      return url.split('?')[0];
    } catch {
      return url.split('?')[0];
    }
  }

  private resetPreviewState(): void {
    this.tryOnPreviewUrl = '';
    this.tryOnUsedAi = false;
    this.tryOnMode = 'avatar';
    this.stageImageErrorCount = 0;
  }

  private sortSavedLooks(left: LookModel, right: LookModel): number {
    switch (this.savedLooksSort) {
      case 'oldest':
        return new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime();
      case 'price-desc':
        return right.totalPrice - left.totalPrice;
      case 'price-asc':
        return left.totalPrice - right.totalPrice;
      case 'with-cover':
        return Number(!!right.heroImageUrl) - Number(!!left.heroImageUrl)
          || new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
      case 'style':
        return left.style.localeCompare(right.style) || left.name.localeCompare(right.name);
      case 'recent':
      default:
        return new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime();
    }
  }
  private handleError(error: unknown, fallbackMessage: string): void {
    const httpError = error as HttpErrorResponse;
    this.error = httpError.error?.message || fallbackMessage;
    console.error(error);
  }

  private async handleShareAction(action: () => Promise<boolean>, successMessage: string): Promise<void> {
    const shared = await action();
    if (!shared) {
      this.error = 'Nao foi possivel compartilhar esse resultado agora.';
      return;
    }

    await this.registerSharedLook();
    this.success = successMessage;
    this.error = '';
  }
}





