import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import type { Cart, CartItem } from "../types";
import apiClient from "../api/client";
import { useAuth } from "../hooks/useAuth";
import { getCartStorageKey, getCurrentUserIdFromStorage } from "../utils/cartStorage";

export const CartPage: React.FC = () => {
  const [cart, setCart] = useState<Cart | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login");
      return;
    }
    fetchCart();
  }, [isAuthenticated, navigate, user?.id]);

  const fetchCart = async () => {
    const localCart = buildCartFromLocalStorage();
    setCart(localCart);

    try {
      const response = await apiClient.get("/cart");
      const apiCart = response.data;
      if (apiCart?.items?.length > 0) {
        setCart(apiCart);
      }
    } catch (err) {
      // Keep local cart visible when backend cart endpoint fails.
      setError("");
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const updateQuantity = async (itemId: number, quantity: number) => {
    updateLocalItemQuantity(itemId, quantity);
    setCart(buildCartFromLocalStorage());

    try {
      await apiClient.put(`/cart/items/${itemId}`, { quantity });
    } catch (err) {
      setError("");
      console.error(err);
    }
  };

  const removeItem = async (itemId: number) => {
    removeLocalItem(itemId);
    setCart(buildCartFromLocalStorage());

    try {
      await apiClient.delete(`/cart/items/${itemId}`);
    } catch (err) {
      setError("");
      console.error(err);
    }
  };

  const clearCart = async () => {
    const userId = user?.id ?? getCurrentUserIdFromStorage();
    const cartStorageKey = getCartStorageKey(userId);
    localStorage.setItem(cartStorageKey, JSON.stringify([]));
    window.dispatchEvent(new Event("pm-cart-updated"));
    setCart(buildCartFromLocalStorage());

    try {
      await apiClient.delete("/cart");
    } catch (err) {
      setError("");
      console.error(err);
    }
  };

  const buildCartFromLocalStorage = (): Cart => {
    const userId = user?.id ?? getCurrentUserIdFromStorage();
    const cartStorageKey = getCartStorageKey(userId);
    const raw = localStorage.getItem(cartStorageKey);
    const items: CartItem[] = raw ? JSON.parse(raw) : [];
    const totalPrice = items.reduce(
      (sum, item) => sum + (((item.product?.price ?? item.price) || 0) * item.quantity),
      0
    );

    const now = new Date().toISOString();
    return {
      id: 0,
      userId: 0,
      items,
      totalPrice,
      createdAt: now,
      updatedAt: now
    };
  };

  const updateLocalItemQuantity = (itemId: number, quantity: number) => {
    const userId = user?.id ?? getCurrentUserIdFromStorage();
    const cartStorageKey = getCartStorageKey(userId);
    const raw = localStorage.getItem(cartStorageKey);
    const items: CartItem[] = raw ? JSON.parse(raw) : [];
    const next = items.map(item =>
      item.id === itemId
        ? { ...item, quantity: Math.max(1, quantity), updatedAt: new Date().toISOString() }
        : item
    );
    localStorage.setItem(cartStorageKey, JSON.stringify(next));
    window.dispatchEvent(new Event("pm-cart-updated"));
  };

  const removeLocalItem = (itemId: number) => {
    const userId = user?.id ?? getCurrentUserIdFromStorage();
    const cartStorageKey = getCartStorageKey(userId);
    const raw = localStorage.getItem(cartStorageKey);
    const items: CartItem[] = raw ? JSON.parse(raw) : [];
    localStorage.setItem(
      cartStorageKey,
      JSON.stringify(items.filter(item => item.id !== itemId))
    );
    window.dispatchEvent(new Event("pm-cart-updated"));
  };

  const handleCheckout = () => {
    navigate("/checkout");
  };

  if (isLoading) {
    return <div className="text-center py-12">Loading cart...</div>;
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <h1 className="text-4xl font-bold text-gray-800 mb-8">Shopping Cart</h1>

        {error && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded">
            {error}
          </div>
        )}

        {!cart || cart.items.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg shadow">
            <p className="text-xl text-gray-600 mb-6">Your cart is empty</p>
            <button
              onClick={() => navigate("/products")}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
            >
              Continue Shopping
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Cart Items */}
            <div className="lg:col-span-2">
              <div className="bg-white rounded-lg shadow overflow-hidden">
                {cart.items.map((item: CartItem) => (
                  <div
                    key={item.id}
                    className="p-6 border-b flex items-center justify-between"
                  >
                    <div className="flex-1">
                      <h3 className="font-semibold text-gray-800">
                        {item.product?.name || item.productName || "Product"}
                      </h3>
                      <p className="text-sm text-gray-600">
                        ${((item.product?.price ?? item.price) || 0).toFixed(2)}
                      </p>
                    </div>

                    <div className="flex items-center gap-4">
                      <div className="flex items-center gap-2">
                        <button
                          onClick={() =>
                            updateQuantity(
                              item.id,
                              Math.max(1, item.quantity - 1)
                            )
                          }
                          className="px-3 py-1 border rounded hover:bg-gray-100"
                        >
                          -
                        </button>
                        <span className="px-4 py-1 border rounded">
                          {item.quantity}
                        </span>
                        <button
                          onClick={() =>
                            updateQuantity(item.id, item.quantity + 1)
                          }
                          className="px-3 py-1 border rounded hover:bg-gray-100"
                        >
                          +
                        </button>
                      </div>

                      <button
                        onClick={() => removeItem(item.id)}
                        className="px-4 py-2 bg-red-100 text-red-600 rounded hover:bg-red-200"
                      >
                        Remove
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Order Summary */}
            <div className="lg:col-span-1">
              <div className="bg-white rounded-lg shadow p-6 sticky top-4">
                <h2 className="text-xl font-semibold text-gray-800 mb-6">
                  Order Summary
                </h2>

                <div className="space-y-4 mb-6 border-b pb-4">
                  <div className="flex justify-between">
                    <span className="text-gray-600">Subtotal</span>
                    <span className="font-semibold">
                      ${cart.totalPrice.toFixed(2)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">Shipping</span>
                    <span className="font-semibold">$0.00</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-600">Tax</span>
                    <span className="font-semibold">$0.00</span>
                  </div>
                </div>

                <div className="flex justify-between text-lg font-bold mb-6">
                  <span>Total</span>
                  <span>${cart.totalPrice.toFixed(2)}</span>
                </div>

                <button
                  onClick={handleCheckout}
                  className="w-full px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 mb-3"
                >
                  Proceed to Checkout
                </button>

                <button
                  onClick={clearCart}
                  className="w-full px-6 py-3 bg-gray-100 text-gray-800 rounded-lg font-semibold hover:bg-gray-200"
                >
                  Clear Cart
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
