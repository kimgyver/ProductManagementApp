// User Types
export interface User {
  id: number;
  username: string;
  email: string;
  role: 'customer' | 'admin' | 'distributor';
  verified: boolean;
  createdAt: string;
  defaultShippingAddress1?: string;
  defaultShippingAddress2?: string;
  defaultRecipientName?: string;
  defaultRecipientPhone?: string;
  defaultShippingPostalCode?: string;
}

export interface AuthResponse {
  success: boolean;
  message?: string;
  userId?: number;
  token?: string;
  user?: User;
}

// Product Types
export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string;
  productStatus: string;
  price: number;
  category: string;
  stock: number;
  weightGrams?: number;
  lengthCm?: number;
  widthCm?: number;
  heightCm?: number;
  rating: number;
  reviewCount: number;
  createdAt: string;
  updatedAt: string;
}

// Cart Types
export interface CartItem {
  id: number;
  productId: number;
  product?: Product;
  quantity: number;
  variantKey?: string;
  selectedOptions?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Cart {
  id: number;
  userId: number;
  items: CartItem[];
  totalPrice: number;
  createdAt: string;
  updatedAt: string;
}

// Order Types
export interface OrderItem {
  id: number;
  productId: number;
  quantity: number;
  price: number;
  basePrice: number;
  selectedOptions?: string;
}

export interface Order {
  id: number;
  userId: number;
  items: OrderItem[];
  totalPrice: number;
  status: string;
  recipientName: string;
  recipientPhone: string;
  shippingAddress1: string;
  shippingAddress2?: string;
  shippingPostalCode: string;
  paymentMethod: string;
  paymentIntentId?: string;
  poNumber?: string;
  paymentDueDate?: string;
  paidAt?: string;
  createdAt: string;
  updatedAt: string;
  shippedat?: string;
  deliveredAt?: string;
  trackingNumber?: string;
}

// Payment Types
export interface ProcessPaymentRequest {
  orderId: number;
  amount: number;
  cardToken?: string;
  paymentMethod?: string;
}

export interface PaymentResponse {
  success: boolean;
  paymentIntentId?: string;
  message?: string;
}

export interface PaymentStatus {
  status: string;
  amount: number;
  paymentIntentId?: string;
  paidAt?: string;
}

// Admin Dashboard Types
export interface DashboardStats {
  totalUsers: number;
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  recentOrders: OrderSummary[];
  topProducts: ProductSales[];
}

export interface OrderSummary {
  id: number;
  userEmail: string;
  totalPrice: number;
  status: string;
  createdAt: string;
}

export interface ProductSales {
  productId: number;
  productName: string;
  salesCount: number;
  revenue: number;
}
