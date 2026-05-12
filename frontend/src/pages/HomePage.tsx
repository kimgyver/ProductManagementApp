import React from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export const HomePage: React.FC = () => {
  const { isAuthenticated } = useAuth();

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      {/* Hero Section */}
      <div className="max-w-7xl mx-auto px-4 py-20">
        <div className="text-center mb-16">
          <h1 className="text-5xl md:text-6xl font-bold text-gray-800 mb-6">
            Welcome to E-Commerce
          </h1>
          <p className="text-xl text-gray-600 mb-8 max-w-2xl mx-auto">
            Discover amazing products with fast delivery and secure payments
          </p>

          <div className="flex gap-4 justify-center">
            <Link
              to="/products"
              className="px-8 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition-colors"
            >
              Shop Now
            </Link>

            {!isAuthenticated && (
              <Link
                to="/register"
                className="px-8 py-3 bg-white text-blue-600 border-2 border-blue-600 rounded-lg font-semibold hover:bg-blue-50 transition-colors"
              >
                Get Started
              </Link>
            )}
          </div>
        </div>

        {/* Features Section */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mt-20">
          <div className="bg-white p-8 rounded-lg shadow-lg text-center">
            <div className="text-4xl mb-4">📦</div>
            <h3 className="text-xl font-semibold text-gray-800 mb-4">
              Fast Delivery
            </h3>
            <p className="text-gray-600">
              Get your orders delivered quickly and safely to your doorstep
            </p>
          </div>

          <div className="bg-white p-8 rounded-lg shadow-lg text-center">
            <div className="text-4xl mb-4">💳</div>
            <h3 className="text-xl font-semibold text-gray-800 mb-4">
              Secure Payments
            </h3>
            <p className="text-gray-600">
              Your payments are protected with the latest security technology
            </p>
          </div>

          <div className="bg-white p-8 rounded-lg shadow-lg text-center">
            <div className="text-4xl mb-4">🛒</div>
            <h3 className="text-xl font-semibold text-gray-800 mb-4">
              Easy Shopping
            </h3>
            <p className="text-gray-600">
              Browse, compare, and purchase with our intuitive interface
            </p>
          </div>
        </div>

        {/* Featured Products Section */}
        <div className="mt-20">
          <h2 className="text-3xl font-bold text-gray-800 mb-8">
            Featured Products
          </h2>
          <div className="bg-white p-12 rounded-lg shadow-lg text-center">
            <p className="text-gray-600 mb-6">
              Browse our collection of popular products
            </p>
            <Link
              to="/products"
              className="inline-block px-6 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition-colors"
            >
              View All Products
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
