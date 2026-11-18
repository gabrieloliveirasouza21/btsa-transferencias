const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'medium' });

export const formatMoney = (value: number) => currency.format(value);

export const formatDateTime = (value: string | null) => (value ? dateTime.format(new Date(value)) : '—');
