import React from 'react';
import { Typography, Card, Table, Space } from 'antd';
import { UserOutlined } from '@ant-design/icons';

const { Title } = Typography;

const AdminUserManagement = () => {
    // TODO: Implement user management
    const columns = [
        {
            title: 'Tên',
            dataIndex: 'name',
            key: 'name',
        },
        {
            title: 'Email',
            dataIndex: 'email',
            key: 'email',
        },
        {
            title: 'Vai trò',
            dataIndex: 'role',
            key: 'role',
        },
        {
            title: 'Trạng thái',
            dataIndex: 'status',
            key: 'status',
        },
        {
            title: 'Hành động',
            key: 'actions',
            render: () => (
                <Space>
                    {/* Action buttons */}
                </Space>
            ),
        },
    ];

    return (
        <div>
            <Title level={2}>
                <UserOutlined /> Quản lý Người dùng
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

export default AdminUserManagement;

