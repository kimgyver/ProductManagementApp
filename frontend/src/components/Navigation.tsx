import React, { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

const CART_STORAGE_KEY = "pm_cart_items";

export const Navigation: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const [cartCount, setCartCount] = useState(0);

  useEffect(() => {
    const syncCartCount = () => {
      const raw = localStorage.getItem(CART_STORAGE_KEY);
      const items = raw ? JSON.parse(raw) : [];
      const count = items.reduce((sum: number, item: { quantity?: number }) => sum + (item.quantity || 0), 0);
      setCartCount(count);
    };

    syncCartCount();

    window.addEventListener("storage", syncCartCount);
    window.addEventListener("pm-cart-updated", syncCartCount as EventListener);
    return () => {
      window.removeEventListener("storage", syncCartCount);
      window.removeEventListener("pm-cart-updated", syncCartCount as EventListener);
    };
  }, []);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="bg-white shadow-md">
      <div className="max-w-7xl mx-auto px-4 py-4 flex items-center justify-between">
        <Link to="/" className="text-2xl font-bold text-blue-600">
          E-Commerce
        </Link>

        <div className="flex items-center gap-6">
          <Link to="/products" className="text-gray-700 hover:text-blue-600">
            Products
          </Link>

          {isAuthenticated ? (
            <>
              <Link
                to="/cart"
                className="text-gray-700 hover:text-blue-600 relative"
              >
                Cart
                <span className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs">
                  {cartCount}
                </span>
              </Link>

              <Link to="/orders" className="text-gray-700 hover:text-blue-600">
                Orders
              </Link>

              {user?.role === "admin" && (
                <Link
                  to="/admin"
                  className="text-gray-700 hover:text-blue-600 font-semibold text-blue-600"
                >
                  Admin
                </Link>
              )}

              <div className="flex items-center gap-3 pl-6 border-l border-gray-300">
                <span className="text-sm text-gray-600">{user?.username}</span>
                <button
                  onClick={handleLogout}
                  className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
                >
                  Logout
                </button>
              </div>
            </>
          ) : (
            <>
              <Link to="/login" className="text-gray-700 hover:text-blue-600">
                Login
              </Link>
              <Link
                to="/register"
                className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Register
              </Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};
