import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import type { Product } from "../types";
import apiClient from "../api/client";
import { useAuth } from "../hooks/useAuth";
import {
  getCartStorageKey,
  getCurrentUserIdFromStorage
} from "../utils/cartStorage";

interface ProductCardProps {
  product: Product;
}

export const ProductCard: React.FC<ProductCardProps> = ({ product }) => {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const [isAdding, setIsAdding] = useState(false);
  const [feedback, setFeedback] = useState("");

  const discountPercent =
    Math.random() < 0.3 ? Math.floor(Math.random() * 30) + 10 : 0;
  const discountedPrice = product.price * (1 - discountPercent / 100);

  const handleAddToCart = async (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();
    e.stopPropagation();

    if (!isAuthenticated) {
      navigate("/login");
      return;
    }

    try {
      setIsAdding(true);
      setFeedback("");
      await apiClient.post("/cart/items", {
        productId: product.id,
        quantity: 1
      });
      addToLocalCart(product);
      setFeedback("Added to cart");
    } catch (err) {
      console.error(err);
      // Keep UI usable even when backend cart persistence is unstable.
      addToLocalCart(product);
      setFeedback("Added locally");
    } finally {
      setIsAdding(false);
      window.setTimeout(() => setFeedback(""), 1500);
    }
  };

  const addToLocalCart = (itemProduct: Product) => {
    const now = new Date().toISOString();
    const userId = user?.id ?? getCurrentUserIdFromStorage();
    const cartStorageKey = getCartStorageKey(userId);
    const raw = localStorage.getItem(cartStorageKey);
    const items = raw ? JSON.parse(raw) : [];

    const existing = items.find(
      (it: { productId: number }) => it.productId === itemProduct.id
    );
    if (existing) {
      existing.quantity = (existing.quantity || 0) + 1;
      existing.updatedAt = now;
    } else {
      items.push({
        id: Date.now(),
        productId: itemProduct.id,
        productName: itemProduct.name,
        price: itemProduct.price,
        quantity: 1,
        createdAt: now,
        updatedAt: now
      });
    }

    localStorage.setItem(cartStorageKey, JSON.stringify(items));
    window.dispatchEvent(new Event("pm-cart-updated"));
  };

  return (
    <div className="bg-white rounded-lg shadow hover:shadow-lg transition-shadow overflow-hidden h-full flex flex-col">
      <div className="p-4 flex-1 flex flex-col">
        <h3 className="text-lg font-semibold text-gray-800 line-clamp-2 mb-2">
          {product.name}
        </h3>

        <p className="text-sm text-gray-600 line-clamp-2 mb-4 flex-1">
          {product.description}
        </p>

        <div className="flex items-center gap-2 mb-3">
          <span className="text-sm text-gray-500 bg-gray-100 px-2 py-1 rounded">
            {product.category}
          </span>
        </div>

        <div className="flex items-center justify-between mb-3">
          <div className="flex flex-col">
            <span className="text-lg font-bold text-blue-600">
              ${discountedPrice.toFixed(2)}
            </span>
            {discountPercent > 0 && (
              <span className="text-xs text-gray-500 line-through">
                ${product.price.toFixed(2)}
              </span>
            )}
          </div>
          {discountPercent > 0 && (
            <span className="text-sm font-semibold text-red-600 bg-red-50 px-2 py-1 rounded">
              -{discountPercent}%
            </span>
          )}
        </div>

        <div className="flex items-center gap-2 mb-3">
          <div className="flex text-yellow-400">
            {"★".repeat(Math.round(product.rating))}
            {"☆".repeat(5 - Math.round(product.rating))}
          </div>
          <span className="text-xs text-gray-600">
            ({product.reviewCount} reviews)
          </span>
        </div>

        <button
          className="w-full bg-blue-600 text-white py-2 rounded-lg font-semibold hover:bg-blue-700 transition-colors"
          onClick={handleAddToCart}
          disabled={isAdding || product.stock === 0}
        >
          {isAdding ? "Adding..." : "Add to Cart"}
        </button>
        {feedback && (
          <p className="text-xs text-center mt-2 text-gray-600">{feedback}</p>
        )}
      </div>

      {product.stock === 0 && (
        <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
          <span className="text-white font-bold text-lg">Out of Stock</span>
        </div>
      )}
    </div>
  );
};
