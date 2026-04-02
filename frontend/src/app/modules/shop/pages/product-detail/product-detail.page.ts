import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { IonicModule } from '@ionic/angular';
import { CartService } from '../../services/cart.service';
import { LoadingSpinnerComponent } from 'src/app/shared/components/loading-spinner/loading-spinner.component';
import { CurrencyPipe } from 'src/app/shared/pipes/currency.pipe';
import { ProductModel, ShopService } from '../../services/shop.service';

@Component({
  selector: 'app-product-detail',
  templateUrl: './product-detail.page.html',
  styleUrls: ['./product-detail.page.scss'],
  standalone: true,
  imports: [CommonModule, IonicModule, LoadingSpinnerComponent, CurrencyPipe]
})
export class ProductDetailPage implements OnInit {
  productId: string | null = '';
  product: ProductModel | null = null;
  activeImageUrl = '';
  loading = false;
  error = '';
  success = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private shopService: ShopService,
    private cartService: CartService
  ) {}

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    this.loadProductDetail();
  }

  async loadProductDetail(): Promise<void> {
    if (!this.productId) {
      this.error = 'Produto invalido.';
      return;
    }

    this.loading = true;
    this.error = '';
    try {
      this.product = await firstValueFrom(this.shopService.getProductById(this.productId));
      this.activeImageUrl = this.product.imageUrls?.[0] || this.product.imageUrl;
    } catch (err) {
      this.error = 'Falha ao carregar detalhes do produto.';
      console.error(err);
    } finally {
      this.loading = false;
    }
  }

  addToCart(): void {
    if (!this.product) {
      return;
    }

    this.cartService.addProduct(this.product);
    this.success = 'Produto adicionado ao carrinho.';
  }

  buyNow(): void {
    if (this.product) {
      this.cartService.addProduct(this.product);
    }

    this.router.navigateByUrl('/shop/checkout');
  }

  browseSimilar(): void {
    if (!this.product) {
      return;
    }

    this.router.navigate(['/shop/list'], {
      queryParams: { category: this.product.category }
    });
  }

  selectImage(imageUrl: string): void {
    this.activeImageUrl = imageUrl;
  }

  openStore(): void {
    const targetUrl = this.product?.productUrl || this.product?.storeUrl;
    if (!targetUrl) {
      this.error = 'Nao encontramos o link externo dessa peca.';
      return;
    }

    window.open(targetUrl, '_blank', 'noopener');
  }
}
