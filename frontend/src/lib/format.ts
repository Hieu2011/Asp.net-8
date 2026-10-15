const objCurrencyFormat = new Intl.NumberFormat('vi-VN', {
  style: 'currency',
  currency: 'VND',
  maximumFractionDigits: 0,
});
const objNumberFormat = new Intl.NumberFormat('vi-VN');
const objDateTimeFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' });

export const formatPrice = (decPrice: number) => objCurrencyFormat.format(decPrice);

export const formatNumber = (numValue: number) => objNumberFormat.format(numValue);

export const formatDateTime = (strIso: string) => objDateTimeFormat.format(new Date(strIso));

/** % giảm giá làm tròn, 0 nếu không giảm */
export const calcDiscountPercent = (decPrice: number, decOldPrice?: number) =>
  decOldPrice && decOldPrice > decPrice ? Math.round((1 - decPrice / decOldPrice) * 100) : 0;
