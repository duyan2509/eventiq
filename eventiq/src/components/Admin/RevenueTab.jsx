import React from 'react';
import { Card, Table, Row, Col, Statistic, Select, Space, Empty } from 'antd';
import { DollarOutlined, WalletOutlined, BankOutlined, FileTextOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';

const RevenueTab = ({ reportData, loading, selectedMonth, selectedYear, onMonthChange, onYearChange }) => {
    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    const columns = [
        {
            title: 'Event Name',
            dataIndex: 'eventName',
            key: 'eventName',
            width: 300,
        },
        {
            title: 'Tickets Sold / Total',
            key: 'tickets',
            align: 'center',
            width: 150,
            render: (_, record) => `${record.ticketsSold} / ${record.totalTickets}`,
        },
        {
            title: 'Platform Fee (20%)',
            dataIndex: 'platformFee',
            key: 'platformFee',
            align: 'right',
            render: (value) => formatPrice(value),
            width: 200,
        },
    ];

    return (
        <div>
            {/* Date Filter */}
            <Card style={{ marginBottom: 24 }}>
                <Space>
                    <span>Select Month/Year:</span>
                    <Select
                        value={selectedMonth}
                        onChange={onMonthChange}
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
                        onChange={onYearChange}
                        style={{ width: 120 }}
                    >
                        {Array.from({ length: 5 }, (_, i) => dayjs().year() - 2 + i).map(year => (
                            <Select.Option key={year} value={year}>
                                {year}
                            </Select.Option>
                        ))}
                    </Select>
                </Space>
            </Card>

            {/* Dashboard Cards */}
            <Row gutter={16} style={{ marginBottom: 24 }}>
                <Col span={8}>
                    <Card>
                        <Statistic
                            title="Total Monthly Revenue"
                            value={reportData?.totalMonthlyRevenue || 0}
                            prefix={<DollarOutlined />}
                            formatter={(value) => formatPrice(value)}
                        />
                    </Card>
                </Col>
                <Col span={8}>
                    <Card>
                        <Statistic
                            title="Total Platform Fee (20%)"
                            value={reportData?.totalPlatformFee || 0}
                            prefix={<WalletOutlined />}
                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#3f8600' }}
                        />
                    </Card>
                </Col>
                <Col span={8}>
                    <Card>
                        <Statistic
                            title="Total Amount to Pay Org"
                            value={reportData?.totalOrgAmount || 0}
                            prefix={<BankOutlined />}
                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#cf1322' }}
                        />
                    </Card>
                </Col>
            </Row>

            {/* Revenue Table */}
            <Card>
                <div style={{ marginBottom: 16 }}>
                    <FileTextOutlined /> Event List
                </div>
                <Table
                    columns={columns}
                    dataSource={reportData?.events || []}
                    rowKey="eventId"
                    loading={loading}
                    pagination={{ pageSize: 10 }}
                    scroll={{ x: 800 }}
                    locale={{
                        emptyText: <Empty description="No data" />,
                    }}
                />
            </Card>
        </div>
    );
};

export default RevenueTab;

