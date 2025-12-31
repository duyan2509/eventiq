import React, { useState, useEffect } from 'react';
import { Card, Row, Col, Statistic, Select, Space } from 'antd';
import { DollarOutlined, WalletOutlined, BankOutlined } from '@ant-design/icons';
import { revenueAPI } from '../../services/api';
import dayjs from 'dayjs';

const RevenueReportSection = () => {
    const [loading, setLoading] = useState(true);
    const [reportData, setReportData] = useState(null);
    const [selectedMonth, setSelectedMonth] = useState(dayjs().month() + 1);
    const [selectedYear, setSelectedYear] = useState(dayjs().year());

    useEffect(() => {
        fetchRevenueReport();
    }, [selectedMonth, selectedYear]);

    const fetchRevenueReport = async () => {
        try {
            setLoading(true);
            const data = await revenueAPI.getAdminRevenueReport(selectedMonth, selectedYear);
            setReportData(data);
        } catch (error) {
            console.error('Error fetching revenue report:', error);
        } finally {
            setLoading(false);
        }
    };

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    return (
        <Card title="Revenue Report" style={{ marginTop: 24 }}>
            {/* Date Filter */}
            <div style={{ marginBottom: 24 }}>
                <Space>
                    <span>Select Month/Year:</span>
                    <Select
                        value={selectedMonth}
                        onChange={setSelectedMonth}
                        style={{ width: 120 }}
                    >
                        {Array.from({ length: 12 }, (_, i) => i + 1).map(month => (
                            <Select.Option key={month} value={month}>
                                Month {month}
                            </Select.Option>
                        ))}
                    </Select>
                    <Select
                        value={selectedYear}
                        onChange={setSelectedYear}
                        style={{ width: 120 }}
                    >
                        {Array.from({ length: 5 }, (_, i) => dayjs().year() - 2 + i).map(year => (
                            <Select.Option key={year} value={year}>
                                {year}
                            </Select.Option>
                        ))}
                    </Select>
                </Space>
            </div>

            {/* Dashboard Cards */}
            <Row gutter={16} style={{ marginBottom: 24 }}>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Monthly Revenue"
                            value={reportData?.totalMonthlyRevenue || 0}
                            prefix={<DollarOutlined />}
                            formatter={(value) => formatPrice(value)}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Platform Fee (20%)"
                            value={reportData?.totalPlatformFee || 0}
                            prefix={<WalletOutlined />}
                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#3f8600' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Amount to Pay Org"
                            value={reportData?.totalOrgAmount || 0}
                            prefix={<BankOutlined />}
                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#cf1322' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Pending Payout"
                            value={reportData?.pendingPayoutEventsCount || 0}
                            formatter={(value) => `${value} events`}
                            valueStyle={{ color: '#faad14' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
            </Row>
        </Card>
    );
};

export default RevenueReportSection;

