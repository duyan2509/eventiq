import React from 'react';
import { Typography, Card, Table, Space } from 'antd';
import { UserOutlined } from '@ant-design/icons';

const { Title } = Typography;

const AdminUserManagement = () => {
    // TODO: Implement user management
    const columns = [
        {
            title: 'Name',
            dataIndex: 'name',
            key: 'name',
        },
        {
            title: 'Email',
            dataIndex: 'email',
            key: 'email',
        },
        {
            title: 'Role',
            dataIndex: 'role',
            key: 'role',
        },
        {
            title: 'Status',
            dataIndex: 'status',
            key: 'status',
        },
        {
            title: 'Actions',
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
                <UserOutlined /> User Management
            </Title>
            <Card>
                <p>Feature under development...</p>
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

