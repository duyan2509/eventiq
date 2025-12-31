import React, { useState, useEffect } from 'react';
import { Card, Table, Select, Space, Statistic, Tag, Button, Empty, Image, Popover, Row, Col } from 'antd';
import { DollarOutlined, CheckOutlined, HistoryOutlined, QrcodeOutlined, CloseOutlined } from '@ant-design/icons';
import { payoutAPI } from '../../services/api';
import dayjs from 'dayjs';
import PayoutModal from './PayoutModal';
import PayoutHistoryModal from './PayoutHistoryModal';

const PayoutTab = () => {
    const [loading, setLoading] = useState(true);
    const [payouts, setPayouts] = useState([]);
    const [pagination, setPagination] = useState({ current: 1, pageSize: 10, total: 0 });
    const [selectedMonth, setSelectedMonth] = useState(dayjs().month() + 1);
    const [selectedYear, setSelectedYear] = useState(dayjs().year());
    const [statusFilter, setStatusFilter] = useState(null);
    const [totalPendingAmount, setTotalPendingAmount] = useState(0);
    const [totalPendingEvents, setTotalPendingEvents] = useState(0);
    const [payoutModalVisible, setPayoutModalVisible] = useState(false);
    const [historyModalVisible, setHistoryModalVisible] = useState(false);
    const [selectedPayout, setSelectedPayout] = useState(null);
    const [selectedOrganizationId, setSelectedOrganizationId] = useState(null);

    useEffect(() => {
        fetchPayouts();
        fetchTotalPendingAmount();
    }, [selectedMonth, selectedYear, statusFilter, pagination.current, pagination.pageSize]);

    const fetchPayouts = async () => {
        try {
            setLoading(true);
            const result = await payoutAPI.getPayouts(
                statusFilter,
                selectedMonth,
                selectedYear,
                pagination.current,
                pagination.pageSize
            );
            setPayouts(result?.data || []);
            setPagination(prev => ({
                ...prev,
                total: result?.total || 0
            }));
        } catch (error) {
            console.error('Error fetching payouts:', error);
        } finally {
            setLoading(false);
        }
    };

    const fetchTotalPendingAmount = async () => {
        try {
            // Get all pending payouts to calculate total
            const result = await payoutAPI.getPayouts('PENDING', selectedMonth, selectedYear, 1, 1000);
            const allPending = result?.data || [];
            const total = allPending.reduce((sum, p) => sum + (p.orgAmount || 0), 0);
            setTotalPendingAmount(total);

            // Count unique events with pending payouts
            const uniqueEvents = new Set(allPending.map(p => p.eventId));
            setTotalPendingEvents(uniqueEvents.size);
        } catch (error) {
            console.error('Error fetching total pending amount:', error);
        }
    };

    const generateVietQRUrl = (payout) => {
        if (!payout?.bankCode || !payout?.accountNumber) {
            return null;
        }
        const amount = payout.orgAmount || 0;
        const accountName = encodeURIComponent(payout.accountName || '');
        const addInfo = encodeURIComponent(`Payout ${payout.eventName}`);
        return `https://img.vietqr.io/image/${payout.bankCode}-${payout.accountNumber}-compact2.png?amount=${amount}&addInfo=${addInfo}&accountName=${accountName}`;
    };

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    const handlePayout = (payout) => {
        setSelectedPayout(payout);
        setPayoutModalVisible(true);
    };

    const handleViewHistory = (organizationId) => {
        setSelectedOrganizationId(organizationId);
        setHistoryModalVisible(true);
    };

    const handlePayoutSuccess = () => {
        setPayoutModalVisible(false);
        setSelectedPayout(null);
        fetchPayouts();
    };

    const columns = [
        {
            title: 'Event Name',
            dataIndex: 'eventName',
            key: 'eventName',
            width: 250,
            render: (text, record) => (
                <Space direction="vertical" size="small">
                    <span>{text}</span>
                    {record.bankCode && record.accountNumber && (
                        <Popover
                            content={
                                <div style={{ textAlign: 'center' }}>
                                    <Image
                                        src={generateVietQRUrl(record)}
                                        alt="VietQR"
                                        width={200}
                                        preview={false}
                                    />
                                    <div style={{ marginTop: 8, fontSize: 12 }}>
                                        <div><strong>Bank:</strong> {record.bankCode}</div>
                                        <div><strong>Account:</strong> {record.accountNumber}</div>
                                        <div><strong>Name:</strong> {record.accountName || '-'}</div>
                                        <div><strong>Amount:</strong> {formatPrice(record.orgAmount || 0)}</div>
                                    </div>
                                </div>
                            }
                            title="Banking Information"
                            trigger="click"
                        >
                            <Button type="link" icon={<QrcodeOutlined />} size="small">
                                View QR
                            </Button>
                        </Popover>
                    )}
                </Space>
            ),
        },
        {
            title: 'Platform Fee (20%)',
            dataIndex: 'platformFee',
            key: 'platformFee',
            align: 'right',
            render: (value) => formatPrice(value),
            width: 180,
        },
        {
            title: 'Org Amount (80%)',
            dataIndex: 'orgAmount',
            key: 'orgAmount',
            align: 'right',
            render: (value) => formatPrice(value),
            width: 180,
        },
        {
            title: 'Payout Status',
            dataIndex: 'status',
            key: 'status',
            align: 'center',
            render: (status) => (
                <Tag color={status === 'PAID' ? 'green' : 'orange'}>
                    {status === 'PAID' ? 'Paid' : 'Pending'}
                </Tag>
            ),
            width: 120,
        },
        {
            title: 'Actions',
            key: 'actions',
            align: 'center',
            render: (_, record) => (
                <Space>
                    {record.status === 'PENDING' && (
                        <Button
                            type="primary"
                            icon={<CheckOutlined />}
                            onClick={() => handlePayout(record)}
                        >
                            Payout
                        </Button>
                    )}
                    <Button
                        icon={<HistoryOutlined />}
                        onClick={() => handleViewHistory(record.organizationId)}
                    >
                        History
                    </Button>
                </Space>
            ),
            width: 200,
        },
    ];

    return (
        <div>
            {/* Filters */}
            <Card style={{ marginBottom: 24 }}>
                <Space>
                    <span>Status:</span>
                    <Select
                        placeholder="All Status"
                        value={statusFilter}
                        onChange={setStatusFilter}
                        allowClear
                        style={{ width: 150 }}
                    >
                        <Select.Option value="PENDING">Pending</Select.Option>
                        <Select.Option value="PAID">Paid</Select.Option>
                    </Select>
                    <span>Month:</span>
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
                    <span>Year:</span>
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
            </Card>
            <Row gutter={16} style={{ marginBottom: 24 }}>
                <Col span={8}>
                    <Card>
                        <Statistic
                            title="Total Pending Payout"
                            value={totalPendingAmount}
                            prefix={<DollarOutlined />}
                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#cf1322' }}
                        />

                    </Card>
                </Col>
                <Col span={8}>
                    <Card>
                    <Statistic
                        title="Total Pending Events"
                        value={totalPendingEvents}
                        prefix={<CloseOutlined />}
                        valueStyle={{ color: '#1890ff' }}
                    />
                    </Card>
                </Col>

            </Row>

            {/* Payouts Table */}
            <Card>
                <Table
                    columns={columns}
                    dataSource={payouts}
                    rowKey="id"
                    loading={loading}
                    pagination={{
                        current: pagination.current,
                        pageSize: pagination.pageSize,
                        total: pagination.total,
                        showSizeChanger: true,
                        showTotal: (total) => `Total ${total} payouts`,
                        onChange: (page, pageSize) => {
                            setPagination(prev => ({ ...prev, current: page, pageSize }));
                        },
                    }}
                    scroll={{ x: 1000 }}
                    locale={{
                        emptyText: <Empty description="No data" />,
                    }}
                />
            </Card>

            {/* Modals */}
            {selectedPayout && (
                <PayoutModal
                    visible={payoutModalVisible}
                    payout={selectedPayout}
                    onSuccess={handlePayoutSuccess}
                    onCancel={() => {
                        setPayoutModalVisible(false);
                        setSelectedPayout(null);
                    }}
                />
            )}

            {selectedOrganizationId && (
                <PayoutHistoryModal
                    visible={historyModalVisible}
                    organizationId={selectedOrganizationId}
                    onClose={() => {
                        setHistoryModalVisible(false);
                        setSelectedOrganizationId(null);
                    }}
                />
            )}
        </div>
    );
};

export default PayoutTab;

