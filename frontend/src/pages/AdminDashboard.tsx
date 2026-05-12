import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import type { DashboardStats } from "../types";
import apiClient from "../api/client";
import { useAuth } from "../hooks/useAuth";

export const AdminDashboard: React.FC = () => {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isAuthenticated || user?.role !== "admin") {
      navigate("/");
      return;
    }
    fetchStats();
  }, [isAuthenticated, user, navigate]);

  const fetchStats = async () => {
    try {
      const response = await apiClient.get("/admin/dashboard");
      setStats(response.data);
    } catch (err) {
      setError("Failed to load dashboard stats");
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return <div className="text-center py-12">Loading dashboard...</div>;
  }

  if (!stats) {
    return <div className="text-center py-12">No data available</div>;
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-7xl mx-auto px-4">
        <h1 className="text-4xl font-bold text-gray-800 mb-8">
          Admin Dashboard
        </h1>

        {error && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded">
            {error}
          </div>
        )}

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white rounded-lg shadow p-6">
            <div className="text-sm font-medium text-gray-600 mb-2">
              Total Users
            </div>
            <div className="text-3xl font-bold text-gray-800">
              {stats.totalUsers}
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="text-sm font-medium text-gray-600 mb-2">
              Total Orders
            </div>
            <div className="text-3xl font-bold text-gray-800">
              {stats.totalOrders}
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="text-sm font-medium text-gray-600 mb-2">
              Total Revenue
            </div>
            <div className="text-3xl font-bold text-green-600">
              ${stats.totalRevenue.toFixed(2)}
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <div className="text-sm font-medium text-gray-600 mb-2">
              Pending Orders
            </div>
            <div className="text-3xl font-bold text-yellow-600">
              {stats.pendingOrders}
            </div>
          </div>
        </div>

        {/* Order Status */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">
              Order Status Overview
            </h2>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Completed</span>
                <span className="font-semibold">{stats.completedOrders}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Pending</span>
                <span className="font-semibold">{stats.pendingOrders}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Cancelled</span>
                <span className="font-semibold">{stats.cancelledOrders}</span>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-xl font-semibold text-gray-800 mb-4">
              Revenue Metrics
            </h2>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Average Order Value</span>
                <span className="font-semibold">
                  $
                  {stats.totalOrders > 0
                    ? (stats.totalRevenue / stats.totalOrders).toFixed(2)
                    : "0.00"}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Total Orders</span>
                <span className="font-semibold">{stats.totalOrders}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-gray-600">Conversion Rate</span>
                <span className="font-semibold">
                  {stats.totalUsers > 0
                    ? ((stats.totalOrders / stats.totalUsers) * 100).toFixed(1)
                    : "0"}
                  %
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Recent Orders */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <div className="p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-800">
                Recent Orders
              </h2>
            </div>
            <div className="divide-y">
              {stats.recentOrders.slice(0, 5).map(order => (
                <div
                  key={order.id}
                  className="p-4 flex justify-between items-center"
                >
                  <div>
                    <p className="font-semibold text-gray-800">
                      Order #{order.id}
                    </p>
                    <p className="text-sm text-gray-600">{order.userEmail}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold">
                      ${order.totalPrice.toFixed(2)}
                    </p>
                    <p className="text-sm text-gray-600">{order.status}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Top Products */}
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <div className="p-6 border-b">
              <h2 className="text-xl font-semibold text-gray-800">
                Top Products
              </h2>
            </div>
            <div className="divide-y">
              {stats.topProducts.slice(0, 5).map(product => (
                <div
                  key={product.productId}
                  className="p-4 flex justify-between items-center"
                >
                  <div>
                    <p className="font-semibold text-gray-800">
                      {product.productName}
                    </p>
                    <p className="text-sm text-gray-600">
                      {product.salesCount} sales
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="font-semibold">
                      ${product.revenue.toFixed(2)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
