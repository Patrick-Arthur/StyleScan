import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'currencyFormat',
   standalone: true // 👈 ISSO AQUI
})
export class CurrencyPipe implements PipeTransform {

  transform(value: number, currencyCode: string = 'BRL', display: 'symbol' | 'code' | 'symbol-narrow' | boolean = 'symbol', digitsInfo: string = '1.2-2'): string {
    if (value == null) return '';

    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: currencyCode,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  }

}
