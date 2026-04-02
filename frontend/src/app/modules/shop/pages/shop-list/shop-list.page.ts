import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { IonicModule } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { LoadingSpinnerComponent } from 'src/app/shared/components/loading-spinner/loading-spinner.component';
import { CurrencyPipe } from 'src/app/shared/pipes/currency.pipe';
import { CartService } from '../../services/cart.service';
import { ProductModel, ShopService } from '../../services/shop.service';

@Component({
  selector: 'app-shop-list',
  templateUrl: './shop-list.page.html',
  styleUrls: ['./shop-list.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, IonicModule, LoadingSpinnerComponent, CurrencyPipe]
})
export class ShopListPage implements OnInit {
  products: ProductModel[] = [];
  loading = false;
  error = '';

  selectedCategory = '';
  selectedPriceRange = '';

  readonly categoryOptions = [
    { value: '', label: 'Todos' },
    { value: 'top', label: 'Tops' },
    { value: 'bottom', label: 'Bottoms' },
    { value: 'shoes', label: 'Shoes' },
    { value: 'dress', label: 'Dress' },
    { value: 'accessory', label: 'Acessorios' }
  ];

  readonly priceOptions = [
    { value: '', label: 'Qualquer preco' },
    { value: '0-180', label: 'Ate R$ 180' },
    { value: '181-280', label: 'R$ 181 a R$ 280' },
    { value: '281-450', label: 'R$ 281 a R$ 450' }
  ];

  constructor(
    private shopService: ShopService,
    private cartService: CartService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      this.selectedCategory = params.get('category') || '';
      this.loadProducts();
    });
  }

  async loadProducts(): Promise<void> {
    this.loading = true;
    this.error = '';

    const { minPrice, maxPrice } = this.parsePriceRange(this.selectedPriceRange);

    try {
      const response = await firstValueFrom(
        this.shopService.getProducts(this.selectedCategory || undefined, minPrice, maxPrice)
      );
      this.products = response.data;
    } catch (err) {
      this.error = 'Falha ao carregar produtos.';
      console.error(err);
    } finally {
      this.loading = false;
    }
  }

  applyFilters(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: this.selectedCategory || null },
      queryParamsHandling: 'merge'
    });

    void this.loadProducts();
  }

  clearFilters(): void {
    this.selectedCategory = '';
    this.selectedPriceRange = '';
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { category: null },
      queryParamsHandling: 'merge'
    });
    void this.loadProducts();
  }

  goHome(): void {
    this.router.navigateByUrl('/home');
  }

  viewProductDetail(productId: string): void {
    this.router.navigateByUrl(`/shop/product/${productId}`);
  }

  addToCart(product: ProductModel): void {
    this.cartService.addProduct(product);
  }

  openStore(product: ProductModel): void {
    const targetUrl = product.productUrl || product.storeUrl;
    if (!targetUrl) {
      return;
    }

    window.open(targetUrl, '_blank', 'noopener');
  }

  trackByProduct(_: number, product: ProductModel): string {
    return product.id;
  }

  private parsePriceRange(range: string): { minPrice?: number; maxPrice?: number } {
    if (!range) {
      return {};
    }

    const [min, max] = range.split('-').map(value => Number(value));
    return {
      minPrice: Number.isFinite(min) ? min : undefined,
      maxPrice: Number.isFinite(max) ? max : undefined
    };
  }
}
