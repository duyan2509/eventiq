import React from 'react';
import { Typography, Card, Table, Space } from 'antd';
import { DollarOutlined } from '@ant-design/icons';

const { Title } = Typography;

const AdminRevenueManagement = () => {
    // TODO: Implement revenue/checkout management
    const columns = [
        {
            title: 'Event',
            dataIndex: 'eventName',
            key: 'eventName',
        },
        {
            title: 'Tổng doanh thu',
            dataIndex: 'revenue',
            key: 'revenue',
            render: (value) => `${value?.toLocaleString('vi-VN')} ₫`,
        },
        {
            title: 'Số lượng vé đã bán',
            dataIndex: 'ticketsSold',
            key: 'ticketsSold',
        },
        {
            title: 'Trạng thái',
            dataIndex: 'status',
            key: 'status',
        },
    ];

    return (
        <div>
            <Title level={2}>
                <DollarOutlined /> Quản lý Doanh thu / Checkout
            </Title>
            <Card>
                <p>Chức năng đang được phát triển...</p>
                <Table
                    columns={columns}
                    dataSource={[]}
                    rowKey="id"
                    pagination={false}
                />
            </Card>
        </div>
    );
};

export default AdminRevenueManagement;

