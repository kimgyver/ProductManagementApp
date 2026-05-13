import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import type { Cart, CartItem } from "../types";
import apiClient from "../api/client";
import { useAuth } from "../hooks/useAuth";
import {
  getCartStorageKey,
  getCurrentUserIdentityFromStorage
} from "../utils/cartStorage";

export const CheckoutPage: React.FC = () => {
  const [cart, setCart] = useState<Cart | null>(null);
  const [recipientName, setRecipientName] = useState("");
  const [recipientPhone, setRecipientPhone] = useState("");
  const [address1, setAddress1] = useState("");
  const [address2, setAddress2] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("card");
  const [cardToken, setCardToken] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [error, setError] = useState("");
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login");
      return;
    }
    fetchCart();
  }, [isAuthenticated, navigate, user?.id, user?.email]);

  const buildCartFromLocalStorage = (): Cart => {
    const userIdentity =
      user?.email ?? user?.id ?? getCurrentUserIdentityFromStorage();
    const cartStorageKey = getCartStorageKey(userIdentity);
    const raw = localStorage.getItem(cartStorageKey);
    const items: CartItem[] = raw ? JSON.parse(raw) : [];
    const totalPrice = items.reduce(
      (sum, item) =>
        sum + ((item.product?.price ?? item.price) || 0) * item.quantity,
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

  const clearLocalCart = () => {
    const userIdentity =
      user?.email ?? user?.id ?? getCurrentUserIdentityFromStorage();
    const cartStorageKey = getCartStorageKey(userIdentity);
    localStorage.setItem(cartStorageKey, JSON.stringify([]));
    window.dispatchEvent(new Event("pm-cart-updated"));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setIsProcessing(true);

    try {
      if (!cart || cart.items.length === 0) {
        setError("Your cart is empty");
        return;
      }

      // Validate items have productId
      const itemsToSend = cart.items.map(item => ({
        productId: item.productId || (item as any).product?.id,
        quantity: item.quantity,
        selectedOptions: item.selectedOptions
      }));

      if (itemsToSend.some(item => !item.productId || item.quantity <= 0)) {
        setError(
          "Invalid cart items. Please try re-adding items to your cart."
        );
        return;
      }

      console.log("Sending order request with items:", itemsToSend);

      // Step 1: Create order
      const orderResponse = await apiClient.post("/orders", {
        items: itemsToSend,
        recipientName,
        recipientPhone,
        shippingAddress1: address1,
        shippingAddress2: address2,
        shippingPostalCode: postalCode,
        paymentMethod
      });

      console.log("Order response:", orderResponse.data);
      const orderId = orderResponse.data.id;

      // Step 2: Process payment
      const paymentResponse = await apiClient.post("/payments/process", {
        orderId,
        amount: cart.totalPrice,
        cardToken: cardToken || "mock_token",
        paymentMethod
      });

      if (paymentResponse.data.success) {
        clearLocalCart();
        await apiClient.delete("/cart");
        navigate(`/orders/${orderId}?status=success`);
      } else {
        setError("Payment failed. Please try again.");
      }
    } catch (err) {
      let errorMessage = "Checkout failed";
      if (axios.isAxiosError(err)) {
        console.error("Axios error response:", err.response?.data);
        // Try to extract detailed error message
        if (err.response?.data) {
          const data = err.response.data;
          if (typeof data.message === "string") {
            errorMessage = data.message;
          } else if (typeof data.error === "string") {
            errorMessage = data.error;
          } else if (data.details) {
            errorMessage = Array.isArray(data.details)
              ? data.details.join("; ")
              : String(data.details);
          }
        }
        if (!errorMessage || errorMessage === "Checkout failed") {
          errorMessage = err.message || "Order creation failed";
        }
      } else if (err instanceof Error) {
        errorMessage = err.message;
      }
      setError(errorMessage);
      console.error("Full error:", err);
    } finally {
      setIsProcessing(false);
    }
  };

  if (isLoading) {
    return <div className="text-center py-12">Loading checkout...</div>;
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-2xl mx-auto px-4">
          <div className="text-center py-12 bg-white rounded-lg shadow">
            <p className="text-xl text-gray-600 mb-6">Your cart is empty</p>
            <button
              onClick={() => navigate("/products")}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
            >
              Continue Shopping
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4">
        <h1 className="text-4xl font-bold text-gray-800 mb-8">Checkout</h1>

        {error && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2">
            <form onSubmit={handleSubmit} className="space-y-8">
              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-2xl font-semibold text-gray-800 mb-6">
                  Shipping Address
                </h2>

                <div className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Recipient Name
                    </label>
                    <input
                      type="text"
                      value={recipientName}
                      onChange={e => setRecipientName(e.target.value)}
                      required
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Phone Number
                    </label>
                    <input
                      type="tel"
                      value={recipientPhone}
                      onChange={e => setRecipientPhone(e.target.value)}
                      required
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Address Line 1
                    </label>
                    <input
                      type="text"
                      value={address1}
                      onChange={e => setAddress1(e.target.value)}
                      required
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Address Line 2 (Optional)
                    </label>
                    <input
                      type="text"
                      value={address2}
                      onChange={e => setAddress2(e.target.value)}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Postal Code
                    </label>
                    <input
                      type="text"
                      value={postalCode}
                      onChange={e => setPostalCode(e.target.value)}
                      required
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    />
                  </div>
                </div>
              </div>

              <div className="bg-white rounded-lg shadow p-6">
                <h2 className="text-2xl font-semibold text-gray-800 mb-6">
                  Payment Method
                </h2>

                <div className="space-y-4">
                  <div>
                    <label
                      className="flex items-center gap-3 p-4 border-2 border-transparent rounded-lg cursor-pointer hover:bg-gray-50 transition-colors"
                      style={{
                        borderColor:
                          paymentMethod === "card" ? "#2563eb" : undefined
                      }}
                    >
                      <input
                        type="radio"
                        name="paymentMethod"
                        value="card"
                        checked={paymentMethod === "card"}
                        onChange={e => setPaymentMethod(e.target.value)}
                      />
                      <span className="font-semibold text-gray-800">
                        Credit/Debit Card
                      </span>
                    </label>
                  </div>

                  {paymentMethod === "card" && (
                    <div className="p-4 bg-blue-50 rounded-lg">
                      <input
                        type="text"
                        placeholder="Mock card token (for demo)"
                        value={cardToken}
                        onChange={e => setCardToken(e.target.value)}
                        className="w-full px-4 py-2 border border-gray-300 rounded-lg"
                      />
                      <p className="text-xs text-gray-600 mt-2">
                        This is a demo. In production, use Stripe or similar.
                      </p>
                    </div>
                  )}
                </div>
              </div>

              <button
                type="submit"
                disabled={isProcessing}
                className="w-full px-6 py-3 bg-green-600 text-white rounded-lg font-semibold hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-lg"
              >
                {isProcessing ? "Processing..." : "Complete Purchase"}
              </button>
            </form>
          </div>

          <div className="lg:col-span-1">
            <div className="bg-white rounded-lg shadow p-6 sticky top-4">
              <h2 className="text-xl font-semibold text-gray-800 mb-6">
                Order Summary
              </h2>

              <div className="space-y-4 mb-6 border-b pb-4">
                {cart.items.map(item => (
                  <div key={item.id} className="flex justify-between text-sm">
                    <span>
                      {item.product?.name || item.productName || "Product"} x
                      {item.quantity}
                    </span>
                    <span>
                      $
                      {(
                        ((item.product?.price ?? item.price) || 0) *
                        item.quantity
                      ).toFixed(2)}
                    </span>
                  </div>
                ))}
              </div>

              <div className="space-y-2 mb-6">
                <div className="flex justify-between">
                  <span className="text-gray-600">Subtotal</span>
                  <span>${cart.totalPrice.toFixed(2)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Shipping</span>
                  <span>$0.00</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Tax</span>
                  <span>$0.00</span>
                </div>
              </div>

              <div className="flex justify-between text-lg font-bold pt-6 border-t">
                <span>Total</span>
                <span>${cart.totalPrice.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
